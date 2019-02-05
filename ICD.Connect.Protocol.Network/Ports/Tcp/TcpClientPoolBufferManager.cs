﻿using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.SerialBuffers;

namespace ICD.Connect.Protocol.Network.Ports.Tcp
{
	/// <summary>
	/// Creates and manages buffers for each AsyncTcpClient that is generated by a TcpClientPool.
	/// </summary>
	public sealed class TcpClientPoolBufferManager : IDisposable
	{
		public delegate void ClientCompletedSerial(TcpClientPoolBufferManager sender, AsyncTcpClient client, string data);

		/// <summary>
		/// Raised when we finish buffering a complete string from a client.
		/// </summary>
		[PublicAPI]
		public event ClientCompletedSerial OnClientCompletedSerial;

		private readonly Func<ISerialBuffer> m_BufferFactory;
		private readonly Dictionary<AsyncTcpClient, ISerialBuffer> m_Buffers;
		private readonly SafeCriticalSection m_BufferSection;

		private TcpClientPool m_Pool;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="bufferFactory"></param>
		public TcpClientPoolBufferManager(Func<ISerialBuffer> bufferFactory)
		{
			m_BufferFactory = bufferFactory;
			m_Buffers = new Dictionary<AsyncTcpClient, ISerialBuffer>();
			m_BufferSection = new SafeCriticalSection();
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			OnClientCompletedSerial = null;

			SetPool(null);
		}

		/// <summary>
		/// Sets the pool to buffer responses from.
		/// </summary>
		/// <param name="pool"></param>
		[PublicAPI]
		public void SetPool(TcpClientPool pool)
		{
			if (pool == m_Pool)
				return;

			Unsubscribe(m_Pool);

			Clear();
			m_Pool = pool;

			Subscribe(m_Pool);
		}

		/// <summary>
		/// Clears the buffers.
		/// </summary>
		[PublicAPI]
		public void Clear()
		{
			AsyncTcpClient[] keys = m_BufferSection.Execute(() => m_Buffers.Keys.ToArray());
			foreach (AsyncTcpClient key in keys)
				RemoveBuffer(key);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Returns the existing buffer for the given client, or instantiates a new one.
		/// </summary>
		/// <param name="client"></param>
		/// <returns></returns>
		private ISerialBuffer LazyLoadBuffer(AsyncTcpClient client)
		{
			m_BufferSection.Enter();

			try
			{
				if (!m_Buffers.ContainsKey(client))
				{
					ISerialBuffer buffer = m_BufferFactory();
					m_Buffers[client] = buffer;
					Subscribe(buffer);
				}

				return m_Buffers[client];
			}
			finally
			{
				m_BufferSection.Leave();
			}
		}

		/// <summary>
		/// Removes the buffer for the given client.
		/// </summary>
		/// <param name="client"></param>
		private void RemoveBuffer(AsyncTcpClient client)
		{
			m_BufferSection.Enter();

			try
			{
				if (!m_Buffers.ContainsKey(client))
					return;

				Unsubscribe(m_Buffers[client]);

				m_Buffers.Remove(client);
			}
			finally
			{
				m_BufferSection.Leave();
			}
		}

		#endregion

		#region Client Callbacks

		/// <summary>
		/// Subscribe to the pool events.
		/// </summary>
		/// <param name="pool"></param>
		private void Subscribe(TcpClientPool pool)
		{
			if (pool == null)
				return;

			pool.OnClientAdded += PoolOnClientAdded;
			pool.OnClientRemoved += PoolOnClientRemoved;
			pool.OnClientConnectionStateChanged += ClientOnConnectedStateChanged;
			pool.OnClientSerialDataReceived += ClientOnSerialDataReceived;
		}

		/// <summary>
		/// Unsubscribe from the pool events.
		/// </summary>
		/// <param name="pool"></param>
		private void Unsubscribe(TcpClientPool pool)
		{
			if (pool == null)
				return;

			pool.OnClientAdded -= PoolOnClientAdded;
			pool.OnClientRemoved -= PoolOnClientRemoved;
			pool.OnClientConnectionStateChanged -= ClientOnConnectedStateChanged;
			pool.OnClientSerialDataReceived -= ClientOnSerialDataReceived;
		}

		/// <summary>
		/// Called when a TCP Client is removed from the pool.
		/// We remove the associated buffer.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="client"></param>
		private void PoolOnClientRemoved(TcpClientPool sender, AsyncTcpClient client)
		{
			RemoveBuffer(client);
		}

		/// <summary>
		/// Called when a 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="client"></param>
		private void PoolOnClientAdded(TcpClientPool sender, AsyncTcpClient client)
		{
			LazyLoadBuffer(client);
		}

		/// <summary>
		/// Called when a client receives data.
		/// </summary>
		/// <param name="tcpClientPool"></param>
		/// <param name="client"></param>
		/// <param name="data"></param>
		private void ClientOnSerialDataReceived(TcpClientPool tcpClientPool, AsyncTcpClient client, string data)
		{
			LazyLoadBuffer(client).Enqueue(data);
		}

		/// <summary>
		/// Called when a client connects or disconnects..
		/// </summary>
		/// <param name="tcpClientPool"></param>
		/// <param name="client"></param>
		/// <param name="connected"></param>
		private void ClientOnConnectedStateChanged(TcpClientPool tcpClientPool, AsyncTcpClient client, bool connected)
		{
			// If a client disconnects ensure the buffer is clear
			if (!connected)
				LazyLoadBuffer(client).Clear();
		}

		#endregion

		#region Buffer Callbacks

		/// <summary>
		/// Subscribe to the buffer events.
		/// </summary>
		/// <param name="buffer"></param>
		private void Subscribe(ISerialBuffer buffer)
		{
			buffer.OnCompletedSerial += BufferOnCompletedSerial;
		}

		/// <summary>
		/// Unsubscribe from the buffer events.
		/// </summary>
		/// <param name="buffer"></param>
		private void Unsubscribe(ISerialBuffer buffer)
		{
			buffer.OnCompletedSerial -= BufferOnCompletedSerial;
		}

		/// <summary>
		/// Called when the buffer raises a complete data string.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void BufferOnCompletedSerial(object sender, StringEventArgs args)
		{
			AsyncTcpClient client = m_BufferSection.Execute(() => m_Buffers.GetKey(sender as ISerialBuffer));

			ClientCompletedSerial handler = OnClientCompletedSerial;
			if (handler != null)
				handler(this, client, args.Data);
		}

		#endregion
	}
}