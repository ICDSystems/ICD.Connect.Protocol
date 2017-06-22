using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.SerialBuffers;

namespace ICD.Connect.Protocol.Network.Tcp
{
	/// <summary>
	/// The TcpServerBufferManager is reponsible for creating buffers for each new
	/// client, and firing an event when complete data is receieved.
	/// </summary>
	[PublicAPI]
	public sealed class TcpServerBufferManager : IDisposable
	{
		public delegate void ClientCompletedSerialCallback(TcpServerBufferManager sender, uint clientId, string data);

		/// <summary>
		/// Raised when we finish buffering a complete string from a client.
		/// </summary>
		[PublicAPI]
		public event ClientCompletedSerialCallback OnClientCompletedSerial;

		private readonly Func<ISerialBuffer> m_BufferFactory;
		private readonly Dictionary<uint, ISerialBuffer> m_Buffers;
		private readonly SafeCriticalSection m_BufferSection;

		private AsyncTcpServer m_Server;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="bufferFactory"></param>
		public TcpServerBufferManager(Func<ISerialBuffer> bufferFactory)
		{
			m_BufferFactory = bufferFactory;
			m_Buffers = new Dictionary<uint, ISerialBuffer>();
			m_BufferSection = new SafeCriticalSection();
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			OnClientCompletedSerial = null;

			SetServer(null);
		}

		/// <summary>
		/// Sets the server to buffer responses from.
		/// </summary>
		/// <param name="server"></param>
		[PublicAPI]
		public void SetServer(AsyncTcpServer server)
		{
			if (server == m_Server)
				return;

			Unsubscribe(m_Server);

			Clear();
			m_Server = server;

			Subscribe(m_Server);
		}

		/// <summary>
		/// Clears the buffers.
		/// </summary>
		[PublicAPI]
		public void Clear()
		{
			uint[] clientIds = m_BufferSection.Execute(() => m_Buffers.Keys.ToArray());
			foreach (uint clientId in clientIds)
				RemoveBuffer(clientId);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Returns the existing buffer for the given client, or instantiates a new one.
		/// </summary>
		/// <param name="clientId"></param>
		/// <returns></returns>
		private ISerialBuffer LazyLoadBuffer(uint clientId)
		{
			m_BufferSection.Enter();

			try
			{
				if (!m_Buffers.ContainsKey(clientId))
				{
					ISerialBuffer buffer = m_BufferFactory();
					m_Buffers[clientId] = buffer;
					Subscribe(buffer);
				}

				return m_Buffers[clientId];
			}
			finally
			{
				m_BufferSection.Leave();
			}
		}

		/// <summary>
		/// Removes the buffer with the given client id.
		/// </summary>
		/// <param name="clientId"></param>
		private void RemoveBuffer(uint clientId)
		{
			m_BufferSection.Enter();

			try
			{
				if (!m_Buffers.ContainsKey(clientId))
					return;

				Unsubscribe(m_Buffers[clientId]);
				m_Buffers.Remove(clientId);
			}
			finally
			{
				m_BufferSection.Leave();
			}
		}

		#endregion

		#region Server Callbacks

		/// <summary>
		/// Subscribe to the server events.
		/// </summary>
		/// <param name="server"></param>
		private void Subscribe(AsyncTcpServer server)
		{
			if (server == null)
				return;

			server.OnDataReceived += ServerOnDataReceived;
			server.OnSocketStateChange += ServerOnSocketStateChange;
		}

		/// <summary>
		/// Unsubscribe from the server events.
		/// </summary>
		/// <param name="server"></param>
		private void Unsubscribe(AsyncTcpServer server)
		{
			if (server == null)
				return;

			server.OnDataReceived -= ServerOnDataReceived;
			server.OnSocketStateChange -= ServerOnSocketStateChange;
		}

		/// <summary>
		/// Called when we receieve data from a client.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void ServerOnDataReceived(object sender, TcpReceiveEventArgs args)
		{
			LazyLoadBuffer(args.ClientId).Enqueue(args.Data);
		}

		/// <summary>
		/// Called when 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void ServerOnSocketStateChange(object sender, SocketStateEventArgs args)
		{
			if (args.SocketState == SocketStateEventArgs.eSocketStatus.SocketStatusNoConnect)
				RemoveBuffer(args.ClientId);
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
			uint clientUi = m_BufferSection.Execute(() => m_Buffers.GetKey(sender as ISerialBuffer));

			ClientCompletedSerialCallback handler = OnClientCompletedSerial;
			if (handler != null)
				handler(this, clientUi, args.Data);
		}

		#endregion
	}
}
