﻿using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;

namespace ICD.Connect.Protocol.SerialQueues
{
	/// <summary>
	/// AbstractSerialQueue will delay subsequent commands until the previous command
	/// has received a response.
	/// </summary>
	[PublicAPI]
	public abstract class AbstractSerialQueue : ISerialQueue
	{
		public event EventHandler<SerialResponseEventArgs> OnSerialResponse;

		public event EventHandler<SerialDataEventArgs> OnTimeout;

		private ISerialBuffer m_Buffer;

		private readonly PriorityQueue<ISerialData> m_CommandQueue;
		private readonly SafeCriticalSection m_CommandLock;
		private readonly SafeTimer m_TimeoutTimer;
		private readonly IcdStopwatch m_DisconnectedTimer;

		private long m_Timeout = 3000;
		private int m_MaxTimeoutCount = 5;
		private int m_TimeoutCount;

		#region Properties

		/// <summary>
		/// Gets the current port.
		/// </summary>
		public ISerialPort Port { get; private set; }

		/// <summary>
		/// Gets/sets the length of the timeout timer.
		/// </summary>
		public long Timeout { get { return m_Timeout; } set { m_Timeout = value; } }

		/// <summary>
		/// Gets/sets the numer of times to timeout in a row before clearing the queue.
		/// </summary>
		public int MaxTimeoutCount { get { return m_MaxTimeoutCount; } set { m_MaxTimeoutCount = value; } }

		/// <summary>
		/// DisconnectedTime is the number of milliseconds since the last timeout.
		/// DisconnectedTime is reset when data is received from the port.
		/// Returns 0 if no command has timed-out yet.
		/// </summary>
		public long DisconnectedTime { get { return m_DisconnectedTimer.ElapsedMilliseconds; } }

		/// <summary>
		/// Returns the number of queued commands.
		/// </summary>
		public int CommandCount { get { return m_CommandLock.Execute(() => m_CommandQueue.Count); } }

		/// <summary>
		/// Gets if the queue is currently waiting on the response to a command
		/// </summary>
		public bool IsCommandInProgress { get; private set; }

		/// <summary>
		/// Gets the command currently running
		/// </summary>
		public ISerialData CurrentCommand { get; protected set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractSerialQueue()
		{
			m_CommandQueue = new PriorityQueue<ISerialData>();
			m_CommandLock = new SafeCriticalSection();
			m_DisconnectedTimer = new IcdStopwatch();
			m_TimeoutTimer = SafeTimer.Stopped(TimeoutCallback);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Sets the serial port for communication with the device.
		/// </summary>
		/// <param name="port"></param>
		public void SetPort(ISerialPort port)
		{
			Unsubscribe(Port);
			Port = port;
			Subscribe(Port);
		}

		/// <summary>
		/// Sets the buffer for splitting incoming responses from the device.
		/// </summary>
		/// <param name="buffer"></param>
		public void SetBuffer(ISerialBuffer buffer)
		{
			Unsubscribe(m_Buffer);
			m_Buffer = buffer;
			Subscribe(m_Buffer);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public virtual void Dispose()
		{
			OnSerialResponse = null;
			OnTimeout = null;

			m_TimeoutTimer.Dispose();
			m_DisconnectedTimer.Stop();

			SetPort(null);
			SetBuffer(null);
		}

		/// <summary>
		/// Clears the command queue.
		/// </summary>
		public void Clear()
		{
			m_CommandLock.Enter();

			try
			{
				m_CommandQueue.Clear();
				StopTimeoutTimer();
			}
			finally
			{
				m_CommandLock.Leave();
			}
		}

		/// <summary>
		/// Queues data to be sent.
		/// </summary>
		/// <param name="data"></param>
		public void Enqueue(ISerialData data)
		{
			if (data == null)
				throw new ArgumentNullException("data");

			m_CommandLock.Enter();

			try
			{
				m_CommandQueue.Enqueue(data);
				CommandAdded();
			}
			finally
			{
				m_CommandLock.Leave();
			}
		}

		/// <summary>
		/// Uses the comparer to determine if a matching command is already queued.
		/// If true, replace the original command with the new one.
		/// 
		/// This is useful in cases such as ramping volume, where we can collapse:
		///		PowerOn
		///		Volume11
		///		Volume12
		///		MuteOn
		///		Volume13
		/// 
		/// To:
		///		PowerOn
		///		Volume13
		///		MuteOn
		/// </summary>
		/// <param name="data"></param>
		/// <param name="comparer"></param>
		public void Enqueue<T>(T data, Func<T, T, bool> comparer)
			where T : class, ISerialData
		{
			if (data == null)
				throw new ArgumentNullException("data");

			m_CommandLock.Enter();

			try
			{
				Func<ISerialData, bool> removeCallback = d => (d is T) && comparer(data, d as T);
				m_CommandQueue.EnqueueRemove(data, removeCallback);
				CommandAdded();
			}
			finally
			{
				m_CommandLock.Leave();
			}
		}

		public void EnqueuePriority(ISerialData data)
		{
			if (data == null)
				throw new ArgumentNullException("data");

			m_CommandLock.Enter();

			try
			{
				m_CommandQueue.Enqueue(data, 0);
				CommandAdded();
			}
			finally
			{
				m_CommandLock.Leave();
			}
		}

		public void EnqueuePriority(ISerialData data, int priority)
		{
			if (data == null)
				throw new ArgumentNullException("data");

			m_CommandLock.Enter();

			try
			{
				m_CommandQueue.Enqueue(data, priority);
				CommandAdded();
			}
			finally
			{
				m_CommandLock.Leave();
			}
		}

		#endregion

		#region Private Methods

		protected virtual void CommandAdded()
		{
			if (!IsCommandInProgress)
				SendImmediate();
		}

		protected virtual void CommandFinished()
		{
			m_CommandLock.Enter();

			try
			{
				if (!IsCommandInProgress && m_CommandQueue.Count > 0)
					SendImmediate();
			}
			finally
			{
				m_CommandLock.Leave();
			}
		}

		/// <summary>
		/// Bypasses the queue and sends the data immediately.
		/// </summary>
		protected bool SendImmediate()
		{
			m_CommandLock.Enter();

			try
			{
				StartTimeoutTimer();

				CurrentCommand = m_CommandQueue.Dequeue();
				IsCommandInProgress = true;

				try
				{
					if (Port != null)
						return Port.Send(CurrentCommand.Serialize());

					ServiceProvider.GetService<ILoggerService>()
					               .AddEntry(eSeverity.Error, "{0} failed to send data - Port is null", GetType().Name);
					Clear();
					return false;
				}
				catch (ObjectDisposedException)
				{
					Clear();
					return false;
				}
			}
			finally
			{
				m_CommandLock.Leave();
			}
		}

		/// <summary>
		/// Callback for the timer.
		/// </summary>
		private void TimeoutCallback()
		{
			if (!m_DisconnectedTimer.IsRunning)
				m_DisconnectedTimer.Start();

			FinishCommand(command => OnTimeout.Raise(this, new SerialDataEventArgs(command)));
		}

		private void FinishCommand(Action<ISerialData> callback)
		{
			StopTimeoutTimer();
			ISerialData command;

			m_CommandLock.Enter();

			try
			{
				command = CurrentCommand;
				CurrentCommand = null;
				IsCommandInProgress = false;
			}
			finally
			{
				m_CommandLock.Leave();
			}

			try
			{
				// Fire the event to allow devices to prioritize commands.
				callback(command);
			}
			catch (Exception e)
			{
				ServiceProvider.GetService<ILoggerService>()
				               .AddEntry(eSeverity.Error, e, "{0} failed to execute callback - {1}", GetType().Name, e.Message);
			}

			CommandFinished();
		}

		private void StartTimeoutTimer()
		{
			m_TimeoutTimer.Reset(m_Timeout);
		}

		private void StopTimeoutTimer()
		{
			m_TimeoutTimer.Stop();
		}

		#endregion

		#region Port Callbacks

		/// <summary>
		/// Subscribe to the port events.
		/// </summary>
		/// <param name="port"></param>
		private void Subscribe(ISerialPort port)
		{
			if (port == null)
				return;

			port.OnConnectedStateChanged += PortOnConnectedStateChanged;
			port.OnSerialDataReceived += PortSerialDataReceived;
		}

		/// <summary>
		/// Unsubscribe from the port events.
		/// </summary>
		/// <param name="port"></param>
		private void Unsubscribe(ISerialPort port)
		{
			if (port == null)
				return;

			port.OnConnectedStateChanged -= PortOnConnectedStateChanged;
			port.OnSerialDataReceived -= PortSerialDataReceived;
		}

		/// <summary>
		/// Called when the port connection state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void PortOnConnectedStateChanged(object sender, BoolEventArgs args)
		{
			// Clear the command queue if we lose connection.
			if (!args.Data)
			{
				m_Buffer.Clear();
				Clear();
			}
		}

		/// <summary>
		/// Called when the port receives some data.
		/// </summary>
		/// <param name="port"></param>
		/// <param name="args"></param>
		private void PortSerialDataReceived(object port, StringEventArgs args)
		{
			if (m_DisconnectedTimer.IsRunning)
				m_DisconnectedTimer.Reset();

			m_Buffer.Enqueue(args.Data);
		}

		#endregion

		#region Buffer Callbacks

		/// <summary>
		/// Subscribes to the buffer events.
		/// </summary>
		/// <param name="buffer"></param>
		private void Subscribe(ISerialBuffer buffer)
		{
			if (buffer == null)
				return;

			buffer.OnCompletedSerial += BufferCompletedSerial;
		}

		/// <summary>
		/// Unsubscribe from the buffer events.
		/// </summary>
		/// <param name="buffer"></param>
		private void Unsubscribe(ISerialBuffer buffer)
		{
			if (buffer == null)
				return;

			buffer.OnCompletedSerial -= BufferCompletedSerial;
		}

		/// <summary>
		/// Called when the buffer completes a string.
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="args"></param>
		protected virtual void BufferCompletedSerial(object buffer, StringEventArgs args)
		{
			string data = args.Data;
			FinishCommand(command => OnSerialResponse.Raise(this, new SerialResponseEventArgs(command, data)));
		}

		#endregion
	}
}
