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
using System;

namespace ICD.Connect.Protocol.SerialQueues
{
	/// <summary>
	/// AbstractSerialQueue will delay subsequent commands until the previous command
	/// has received a response.
	/// </summary>
	[PublicAPI]
	public abstract class AbstractSerialQueue : ISerialQueue
	{
		/// <summary>
		/// Raised when serial data is sent to the port.
		/// </summary>
		public event EventHandler<SerialTransmissionEventArgs> OnSerialTransmission;

		/// <summary>
		/// Raises individual commands with their responses.
		/// </summary>
		public event EventHandler<SerialResponseEventArgs> OnSerialResponse;

		/// <summary>
		/// Raised when a command does not yield a response within a time limit.
		/// </summary>
		public event EventHandler<SerialDataEventArgs> OnTimeout;

		/// <summary>
		/// Raised when a command fails to send due to port failures.
		/// </summary>
		public event EventHandler<SerialDataEventArgs> OnSendFailed;

		private ISerialBuffer m_Buffer;

		private readonly PriorityQueue<ISerialData> m_CommandQueue;

		/// <summary>
		/// This acts as thread syncronization for both m_CommandQueue and m_CurrentCommand
		/// </summary>
		private readonly SafeCriticalSection m_CommandSection;

		private readonly SafeTimer m_TimeoutTimer;
		private readonly IcdStopwatch m_DisconnectedTimer;

		/// <summary>
		/// Timer for handling command delay
		/// </summary>
		private readonly SafeTimer m_CommandDelayTimer;

		/// <summary>
		/// True when command is ok to send from the command delay (or if there is no delay)
		/// False when the delay hasn't elapsed yet
		/// </summary>
		private bool m_CommandDelayRunning;

		private ISerialData m_CurrentCommand;

		private bool m_CommandIsRunning;

		private readonly SafeTimer m_DisconnectClearTimer;

		#region Properties

		public bool Debug { get; set; }

		/// <summary>
		/// Gets the current port.
		/// </summary>
		public ISerialPort Port { get; private set; }

		/// <summary>
		/// When true the serial queue will ignore responses and immediately start processing the next command.
		/// </summary>
		public bool Trust { get; set; }

		/// <summary>
		/// Gets/sets the length of the timeout timer.
		/// </summary>
		public long Timeout { get; set; }

		/// <summary>
		/// Gets/sets the number of times to timeout in a row before clearing the queue.
		/// </summary>
		public int MaxTimeoutCount { get; set; }

		/// <summary>
		/// Gets the number of times in a row the queue has raised a timeout.
		/// </summary>
		public int TimeoutCount { get; private set; }

		/// <summary>
		/// Wait time between sending commands
		/// </summary>
		public long CommandDelayTime { get; set; }

		/// <summary>
		/// How long to wait for the port to re-connect before clearing the queue and buffer
		/// This is helpful for IP devices that regularly disconnect in operation
		/// </summary>
		public long DisconnectClearTime { get; set; }

		/// <summary>
		/// Returns the number of queued commands.
		/// </summary>
		public int CommandCount { get { return m_CommandSection.Execute(() => m_CommandQueue.Count); } }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractSerialQueue()
		{
			m_CommandDelayTimer = SafeTimer.Stopped(ComandDelayTimerElapesed);
			m_CommandQueue = new PriorityQueue<ISerialData>();
			m_CommandSection = new SafeCriticalSection();
			m_DisconnectedTimer = new IcdStopwatch();
			m_TimeoutTimer = SafeTimer.Stopped(TimeoutCallback);
			m_DisconnectClearTimer = SafeTimer.Stopped(DisconnectedClearCallback);

			MaxTimeoutCount = 5;
			Timeout = 3000;
			CommandDelayTime = 0;
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public virtual void Dispose()
		{
			OnSerialTransmission = null;
			OnSerialResponse = null;
			OnTimeout = null;

			m_TimeoutTimer.Dispose();
			m_DisconnectedTimer.Stop();

			SetPort(null);
			SetBuffer(null);
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
		/// Clears the command queue.
		/// </summary>
		public void Clear()
		{
			if (Debug)
				IcdConsole.PrintLine(eConsoleColor.YellowOnRed, "Clearing Queue!");

			m_CommandSection.Enter();

			try
			{
				m_CommandQueue.Clear();
				m_CurrentCommand = null;
				m_CommandIsRunning = false;
			}
			finally
			{
				m_CommandSection.Leave();
			}

			StopTimeoutTimer();
		}

		/// <summary>
		/// Queues data to be sent.
		/// </summary>
		/// <param name="data"></param>
		public void Enqueue(ISerialData data)
		{
			if (data == null)
				throw new ArgumentNullException("data");

			EnqueuePriority(data, int.MaxValue);
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
		public void Enqueue(ISerialData data, Func<ISerialData, ISerialData, bool> comparer)
		{
			if (data == null)
				throw new ArgumentNullException("data");

			EnqueuePriority(data, comparer, int.MaxValue, false);
		}

		/// <summary>
		/// Enqueues the given data at higher than normal priority.
		/// </summary>
		/// <param name="data"></param>
		public void EnqueuePriority(ISerialData data)
		{
			if (data == null)
				throw new ArgumentNullException("data");

			EnqueuePriority(data, 0);
		}

		/// <summary>
		/// Enqueues the given data with the given priority (lower value is higher priority) 
		/// </summary>
		/// <param name="data"></param>
		/// <param name="priority"></param>
		public void EnqueuePriority(ISerialData data, int priority)
		{
			if (data == null)
				throw new ArgumentNullException("data");

			EnqueuePriority(data, (a, b) => false, priority);
		}

		///  <summary>
		///  Enqueues the given data with the given priority (lower value is higher priority) 
		/// 
		///  Uses the comparer to determine if a matching command is already queued.
		///  If true, replace the original command with the new one.
		///  
		///  This is useful in cases such as ramping volume, where we can collapse:
		/// 		PowerOn
		/// 		Volume11
		/// 		Volume12
		/// 		MuteOn
		/// 		Volume13
		///  
		///  To:
		/// 		PowerOn
		/// 		Volume13
		/// 		MuteOn
		///  </summary>
		///  <param name="data"></param>
		///  <param name="comparer"></param>
		///  <param name="priority"></param>
		public void EnqueuePriority(ISerialData data, Func<ISerialData, ISerialData, bool> comparer, int priority)
		{
			if (data == null)
				throw new ArgumentNullException("data");

			EnqueuePriority(data, comparer, priority, false);
		}

		///  <summary>
		///  Enqueues the given data with the given priority (lower value is higher priority) 
		/// 
		///  Uses the comparer to determine if a matching command is already queued.
		///  If true, replace the original command with the new one.
		///  
		///  This is useful in cases such as ramping volume, where we can collapse:
		/// 		PowerOn
		/// 		Volume11
		/// 		Volume12
		/// 		MuteOn
		/// 		Volume13
		///  
		///  To:
		/// 		PowerOn
		/// 		Volume13
		/// 		MuteOn
		///  </summary>
		///  <param name="data"></param>
		///  <param name="comparer"></param>
		///  <param name="priority"></param>
		///  <param name="deDuplicateToEndOfQueue"></param>
		public void EnqueuePriority(ISerialData data, Func<ISerialData, ISerialData, bool> comparer, int priority, bool deDuplicateToEndOfQueue)
		{
			if (data == null)
				throw new ArgumentNullException("data");

			m_CommandSection.Execute(() =>
			                         m_CommandQueue.EnqueueRemove(data, d => comparer(d, data), priority,
			                                                      deDuplicateToEndOfQueue));
			SendNextCommand();
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Dequeues the next command and sends it.
		/// </summary>
		private void SendNextCommand()
		{
			if (Debug)
				IcdConsole.PrintLine(eConsoleColor.Magenta, "Send Next Command");

			m_CommandSection.Enter();
			try
			{
				if (m_CommandIsRunning || CommandCount == 0 || m_CommandDelayRunning)
				{
					if (!Debug)
						return;
					string reason = m_CommandIsRunning
						                ? "Command In Progress"
						                : CommandCount == 0
							                  ? "0 Commands in Queue"
							                  : "Timer Not Ready";
					
					IcdConsole.PrintLine(eConsoleColor.Magenta, "Command Not Sent {0}", reason);
					return;
				}

				m_CurrentCommand = m_CommandQueue.Dequeue();
				m_CommandIsRunning = true;
				if (Debug)
					IcdConsole.PrintLine(eConsoleColor.Magenta, "Dequeued Command {0}", m_CurrentCommand.Serialize());
			}
			finally
			{
				m_CommandSection.Leave();
			}

			StartTimeoutTimer();

			try
			{
				if (Port == null)
				{
					ServiceProvider.GetService<ILoggerService>()
					               .AddEntry(eSeverity.Error, "{0} failed to send data - Port is null",
					                         GetType().Name);
					Clear();
					return;
				}

				bool sendSuccessful = Port.Send(m_CurrentCommand.Serialize());

				if (!sendSuccessful)
				{
					OnSendFailed.Raise(this, new SerialDataEventArgs(m_CurrentCommand));
					
					if (Debug)
						IcdConsole.PrintLine(eConsoleColor.YellowOnRed, "Send Command Failed!!");
					return;
				}

				ResetComandDelayTimer();

				OnSerialTransmission.Raise(this, new SerialTransmissionEventArgs(m_CurrentCommand));

				if (Trust)
				{
					FinishCommand(command => { });
				}
			}
			catch (ObjectDisposedException)
			{
				if (Debug)
					IcdConsole.PrintLine(eConsoleColor.YellowOnRed, "ObjectDisposedException, clearing queue");
				Clear();
			}
		}

		/// <summary>
		/// Callback for the timer.
		/// </summary>
		private void TimeoutCallback()
		{
			// Don't care about timeouts in trust mode
			if (Trust)
				return;

			if (Debug)
				IcdConsole.PrintLine(eConsoleColor.Magenta, "Timeout Expired - finishing command");

			if (!m_DisconnectedTimer.IsRunning)
			{
				m_DisconnectedTimer.Start();
			}

			TimeoutCount++;

			FinishCommand(command => OnTimeout.Raise(this, new SerialDataEventArgs(command)));
		}

		private void FinishCommand(Action<ISerialData> callback)
		{
			StopTimeoutTimer();

			if (Debug)
			{
				if (m_CurrentCommand != null)
					IcdConsole.PrintLine(eConsoleColor.Magenta, "Finishing Command: {0}", m_CurrentCommand.Serialize());
				else
					IcdConsole.PrintLine(eConsoleColor.Magenta, "Finishing Command - Current Command is Null");
			}

			try
			{
				// Fire the event to allow devices to prioritize commands.
				callback(m_CurrentCommand);
			}
			catch (Exception e)
			{
				ServiceProvider.GetService<ILoggerService>()
				               .AddEntry(eSeverity.Error, e, "{0} failed to execute callback - {1}", GetType().Name, e.Message);
			}

			m_CommandSection.Enter();

			try
			{
				m_CurrentCommand = null;
				m_CommandIsRunning = false;
				if (Debug)
					IcdConsole.PrintLine(eConsoleColor.Magenta, "Clearing Current Command");
			}
			finally
			{
				m_CommandSection.Leave();
			}

			SendNextCommand();
		}

		private void StartTimeoutTimer()
		{
			m_TimeoutTimer.Reset(Timeout);
		}

		private void StopTimeoutTimer()
		{
			m_TimeoutTimer.Stop();
		}

		private void DisconnectedClearCallback()
		{
			m_Buffer.Clear();
			Clear();
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
			if (args.Data)
				m_DisconnectClearTimer.Stop(); // If we're connected, stop the timer
			else if (DisconnectClearTime > 0)
				m_DisconnectClearTimer.Reset(DisconnectClearTime); // Reset the timer if DisconnectClearTime is set > 0
			else
				DisconnectedClearCallback(); // Clear the queue immediately if DisconnectClearTime isn't >0
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

			// Ignore buffer feedback
			if (Trust)
				return;

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
		private void BufferCompletedSerial(object buffer, StringEventArgs args)
		{
			// Ignore buffer feedback
			if (Trust)
				return;

			TimeoutCount = 0;

			string data = args.Data;
			FinishCommand(command => OnSerialResponse.Raise(this, new SerialResponseEventArgs(command, data)));
		}

		#endregion

		#region Rate Limit

		private void ResetComandDelayTimer()
		{
			if (CommandDelayTime == 0)
				return;

			if (Debug)
				IcdConsole.PrintLine(eConsoleColor.Magenta, "Resetting Delay Timer");

			m_CommandSection.Enter();

			try
			{
				if (!m_CommandDelayRunning)
				{
					m_CommandDelayRunning = true;
					m_CommandDelayTimer.Reset(CommandDelayTime);
				}
				else if (Debug)
				{
					IcdConsole.PrintLine(eConsoleColor.Magenta, "Delay Timer Already Running! Possible Threading Issue?");
				}
			}
			finally
			{
				m_CommandSection.Leave();
			}
		}

		private void ComandDelayTimerElapesed()
		{
			m_CommandSection.Execute(() => m_CommandDelayRunning = false);

			SendNextCommand();
		}

		#endregion
	}
}
