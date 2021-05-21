#if !SIMPLSHARP
using System;
using System.IO.Pipes;
using System.Linq;
using ICD.Common.Logging.LoggingContexts;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Network.Ports.NamedPipe.Sockets;
using ICD.Connect.Protocol.Network.Servers;
using ICD.Connect.Settings.Utils;

namespace ICD.Connect.Protocol.Network.Ports.NamedPipe
{
	public sealed class NamedPipeServer : AbstractServer<string>
	{
		private const int DEFAULT_MAX_NUMBER_OF_CLIENTS = 64;

		private readonly BiDictionary<uint, ServerNamedPipeSocket> m_Sockets;
		private readonly SafeCriticalSection m_SocketsSection;

		#region Properties

		/// <summary>
		/// Max number of connections supported by the server.
		/// </summary>
		public int MaxNumberOfClients { get; set; }

		/// <summary>
		/// Gets/sets the pipe name.
		/// </summary>
		public string PipeName { get; set; }

		/// <summary>
		/// Gets/sets the pipe direction.
		/// </summary>
		public PipeDirection Direction { get; set; }

		/// <summary>
		/// Gets/sets the pipe transmission mode.
		/// </summary>
		public PipeTransmissionMode TransmissionMode { get; set; }

		/// <summary>
		/// Gets/sets the pipe options.
		/// </summary>
		public PipeOptions Options { get; set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public NamedPipeServer()
		{
			MaxNumberOfClients = DEFAULT_MAX_NUMBER_OF_CLIENTS;
			Direction = PipeDirection.InOut;
			TransmissionMode = PipeTransmissionMode.Byte;
			Options = PipeOptions.None;

			m_Sockets = new BiDictionary<uint, ServerNamedPipeSocket>();
			m_SocketsSection = new SafeCriticalSection();
		}

		#region Methods

		/// <summary>
		/// Starts the server.
		/// </summary>
		public override void Start()
		{
			Stop(false);

			Enabled = true;
			AddListener();
		}

		/// <summary>
		/// Stops the TCP server.
		/// </summary>
		/// <param name="disable">When true disables the TCP server.</param>
		[PublicAPI]
		protected override void Stop(bool disable)
		{
			int count = m_SocketsSection.Execute(() => m_Sockets.Count);

			if (disable)
			{
				if (count > 0)
					Logger.Log(eSeverity.Notice, "Stopping server");
				Enabled = false;
			}
			else
			{
				if (count > 0)
					Logger.Log(eSeverity.Notice, "Temporarily stopping server");
			}

			if (count > 0)
				Logger.Log(eSeverity.Notice, "No longer listening");

			foreach (uint client in GetClients())
				RemoveSocket(client);

			UpdateListeningState();
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Adds a new socket if the server is enabled and there are no disconnected sockets.
		/// </summary>
		private void AddListener()
		{
			m_SocketsSection.Enter();

			try
			{
				if (Enabled &&
				    m_Sockets.Values.Any(s => !s.IsConnected) &&
				    m_Sockets.Count < MaxNumberOfClients)
					AddSocket();
			}
			finally
			{
				m_SocketsSection.Leave();
			}
		}

		/// <summary>
		/// Adds a new socket and begins listening for a new client connection.
		/// </summary>
		private void AddSocket()
		{
			m_SocketsSection.Enter();

			try
			{
				uint clientId = (uint)IdUtils.GetNewId(m_Sockets.Keys.Select(i => (int)i));

				NamedPipeServerStream stream =
					new NamedPipeServerStream(PipeName,
					                          Direction,
					                          MaxNumberOfClients,
					                          TransmissionMode,
					                          Options,
					                          BufferSize,
					                          BufferSize);

				ServerNamedPipeSocket socket = new ServerNamedPipeSocket(stream, PipeName);
				m_Sockets.Add(clientId, socket);

				Subscribe(socket);

				socket.ConnectAsync();
			}
			finally
			{
				m_SocketsSection.Leave();
			}

			UpdateListeningState();
		}

		/// <summary>
		/// Removes the socket for the given client id.
		/// </summary>
		/// <param name="clientId"></param>
		private void RemoveSocket(uint clientId)
		{
			m_SocketsSection.Enter();

			try
			{
				ServerNamedPipeSocket socket;
				if (m_Sockets.TryGetValue(clientId, out socket))
				{
					Unsubscribe(socket);
					m_Sockets.RemoveKey(clientId);
					socket.Dispose();
				}
			}
			finally
			{
				m_SocketsSection.Leave();
			}

			RemoveClient(clientId, SocketStateEventArgs.eSocketStatus.SocketStatusNoConnect);

			UpdateListeningState();
		}

		/// <summary>
		/// Called in a worker thread to send the data to the specified client
		/// This should send the data synchronously to ensure in-order transmission
		/// If this blocks, it will stop all data from being sent
		/// </summary>
		/// <param name="clientId"></param>
		/// <param name="data"></param>
		protected override void SendWorkerAction(uint clientId, string data)
		{
			byte[] byteData = StringUtils.ToBytes(data);

			string pipeName;
			TryGetClientInfo(clientId, out pipeName);

			PrintTx(pipeName, data);
			Send(clientId, byteData);
		}

		/// <summary>
		/// Sends a Byte for Byte string (ISO-8859-1)
		/// </summary>
		/// <param name="clientId">Client Identifier for Connection</param>
		/// <param name="data">String in ISO-8859-1 Format</param>
		/// <returns></returns>
		private void Send(uint clientId, byte[] data)
		{
			m_SocketsSection.Enter();

			try
			{
				ServerNamedPipeSocket socket;
				if (!m_Sockets.TryGetValue(clientId, out socket) || !socket.IsConnected)
				{
					Logger.Log(eSeverity.Error, "Unable to send data to unconnected client {0}", clientId);
					return;
				}

				socket.SendAsync(data);
			}
			catch (Exception ex)
			{
				Logger.Log(eSeverity.Error, ex, "Failed to send data to client {0}", clientId);
				RemoveSocket(clientId);
			}
			finally
			{
				m_SocketsSection.Leave();
			}

			if (!ClientConnected(clientId))
				RemoveSocket(clientId);
		}

		/// <summary>
		/// Updates the server listening state to reflect the number of active sockets.
		/// </summary>
		private void UpdateListeningState()
		{
			Listening = m_Sockets.Count > 0;
		}

		#endregion

		#region Socket Callbacks

		/// <summary>
		/// Subscribe to the socket events.
		/// </summary>
		/// <param name="socket"></param>
		private void Subscribe(ServerNamedPipeSocket socket)
		{
			socket.OnDataReceived += SocketOnDataReceived;
			socket.OnIsConnectedChanged += SocketOnIsConnectedChanged;
		}

		/// <summary>
		/// Unsubscribe from the socket events.
		/// </summary>
		/// <param name="socket"></param>
		private void Unsubscribe(ServerNamedPipeSocket socket)
		{
			socket.OnDataReceived -= SocketOnDataReceived;
			socket.OnIsConnectedChanged -= SocketOnIsConnectedChanged;
		}

		/// <summary>
		/// Called when a client connects/disconnects.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void SocketOnIsConnectedChanged(object sender, BoolEventArgs eventArgs)
		{
			ServerNamedPipeSocket socket = sender as ServerNamedPipeSocket;
			if (socket == null)
				throw new ArgumentException("Unexpected sender");

			uint clientId;
			if (m_Sockets.TryGetKey(socket, out clientId))
			{
				if (eventArgs.Data)
					AddClient(clientId, socket.PipeName, SocketStateEventArgs.eSocketStatus.SocketStatusConnected);
				else
					RemoveSocket(clientId);
			}

			AddListener();
			UpdateListeningState();
		}

		/// <summary>
		/// Called when data is received from a client.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void SocketOnDataReceived(object sender, GenericEventArgs<byte[]> eventArgs)
		{
			ServerNamedPipeSocket socket = sender as ServerNamedPipeSocket;
			if (socket == null)
				throw new ArgumentException("Unexpected sender");

			uint clientId;
			if (m_Sockets.TryGetKey(socket, out clientId))
				RaiseOnDataReceived(new DataReceiveEventArgs(clientId, eventArgs.Data, eventArgs.Data.Length));

			UpdateListeningState();
		}

		#endregion
	}
}
#endif
