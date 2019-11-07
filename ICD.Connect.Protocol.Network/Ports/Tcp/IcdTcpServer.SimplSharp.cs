#if SIMPLSHARP
using System;
using System.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.Network.Ports.Tcp
{
	public sealed partial class IcdTcpServer
	{
		[CanBeNull]
		private TCPServer m_TcpListener;

		#region Methods

		/// <summary>
		/// Starts the TCP Server
		/// </summary>
		[PublicAPI]
		public void Start()
		{
			if (m_TcpListener != null)
				throw new InvalidOperationException("TCP Server must be stopped before it is started again.");

			Logger.AddEntry(eSeverity.Notice, "{0} - Starting server", this);

			Enabled = true;

			m_TcpListener = new TCPServer(AddressToAcceptConnectionFrom, Port, BufferSize,
			                              EthernetAdapterType.EthernetUnknownAdapter, MaxNumberOfClients);
			m_TcpListener.SocketStatusChange += HandleSocketStatusChange;
			m_TcpListener.WaitForConnectionAsync(AddressToAcceptConnectionFrom, TcpClientConnectCallback);

			// Hack - I think it takes a little while for the listening state to update
			Listening = true;

			Logger.AddEntry(eSeverity.Notice,
			                string.Format("{0} - Listening on port {1} with max # of connections {2}", this, Port,
			                              MaxNumberOfClients));
		}

		/// <summary>
		/// Stops and Disables the TCP Server
		/// </summary>
		public void Stop()
		{
			Stop(true);
		}

		/// <summary>
		/// Stops the TCP server.
		/// </summary>
		/// <param name="disable">When true disables the TCP server.</param>
		private void Stop(bool disable)
		{
			if (disable)
			{
				if (m_TcpListener != null)
					Logger.AddEntry(eSeverity.Notice, "{0} - Stopping server", this);
				Enabled = false;
			}
			else
			{
				if (m_TcpListener != null)
					Logger.AddEntry(eSeverity.Notice, "{0} - Temporarily stopping server", this);
			}

			if (m_TcpListener != null)
			{
				m_TcpListener.SocketStatusChange -= HandleSocketStatusChange;

				try
				{
					m_TcpListener.DisconnectAll();
				}
				catch (Exception e)
				{
					// Handling some internal Crestron exception that occurs when the stream is disposed.
					// SimplSharpPro: Got unhandled exception System.Exception:  Object not initialized
					//	at Crestron.SimplSharp.CEvent.Set()
					//	at Crestron.SimplSharp.AsyncStream.Close()
					//	at Crestron.SimplSharp.CrestronIO.Stream.Dispose()
					//	at Crestron.SimplSharp.CrestronSockets.TCPServer.DisconnectAll()
					if (e.Message == null || !e.Message.Contains("Object not initialized"))
						throw;
				}
				finally
				{
					Logger.AddEntry(eSeverity.Notice, "{0} - No longer listening on port {1}", this, m_TcpListener.PortNumber);

					m_TcpListener = null;
					UpdateListeningState();
				}
			}

			foreach (uint client in GetClients())
				RemoveClient(client, SocketStateEventArgs.eSocketStatus.SocketStatusNoConnect);
		}

		/// <summary>
		/// Sends the data to all connected clients.
		/// </summary>
		/// <param name="data"></param>
		public void Send(string data)
		{
			uint[] clients = GetClients().ToArray();
			if (clients.Length == 0)
				return;

			byte[] byteData = StringUtils.ToBytes(data);

			foreach (uint clientId in clients)
			{
				HostInfo hostInfo = GetClientInfo(clientId);

				PrintTx(hostInfo, data);
				m_TcpListener.SendDataAsync(clientId, byteData, byteData.Length, (tcpListener, clientIndex, bytesCount) => { });
			}
		}

		/// <summary>
		/// Sends a Byte for Byte string (ISO-8859-1)
		/// </summary>
		/// <param name="clientId">Client Identifier for Connection</param>
		/// <param name="data">String in ISO-8859-1 Format</param>
		/// <returns></returns>
		public void Send(uint clientId, string data)
		{
			if (!ClientConnected(clientId))
			{
				Logger.AddEntry(eSeverity.Warning, "{0} - Unable to send data to unconnected client {1}", this, clientId);
				RemoveClient(clientId, SocketStateEventArgs.eSocketStatus.SocketStatusNoConnect);
				return;
			}

			byte[] byteData = StringUtils.ToBytes(data);
			HostInfo hostInfo = GetClientInfo(clientId);

			PrintTx(hostInfo, data);
			m_TcpListener.SendDataAsync(clientId, byteData, byteData.Length, (tcpListener, clientIndex, bytesCount) => { });
		}

		/// <summary>
		/// Gets the address and port for the client with the given id.
		/// </summary>
		/// <param name="client"></param>
		/// <returns></returns>
		[PublicAPI]
		public HostInfo GetClientInfo(uint client)
		{
			if (m_TcpListener == null)
				throw new InvalidOperationException("Server is not connected");

			string address = m_TcpListener.GetAddressServerAcceptedConnectionFromForSpecificClient(client);
			ushort port = (ushort)m_TcpListener.GetPortNumberServerAcceptedConnectionFromForSpecificClient(client);

			return new HostInfo(address, port);
		}

		/// <summary>
		/// Returns true if the client is connected.
		/// </summary>
		/// <param name="client"></param>
		/// <returns></returns>
		public bool ClientConnected(uint client)
		{
			if (m_TcpListener == null)
				return false;

			// This is a hack. We have no way of determining if a client id is still valid,
			// so if we get a null address we know the client is invalid.
			if (m_TcpListener.GetLocalAddressServerAcceptedConnectionFromForSpecificClient(client) == null)
				return false;

			return m_TcpListener.ClientConnected(client);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Fires on a change in socket status
		/// </summary>
		/// <param name="tcpListener"></param>
		/// <param name="clientId"></param>
		/// <param name="status"></param>
		private void HandleSocketStatusChange(TCPServer tcpListener, uint clientId, SocketStatus status)
		{
			SocketStateEventArgs.eSocketStatus reason = GetSocketStatus(status);

			if (clientId != 0)
			{
				// Client disconnected
				if (!ClientConnected(clientId))
				{
					RemoveClient(clientId, reason);
				}
					// Client connected
				else if (!ContainsClient(clientId))
				{
					AddClient(clientId, reason);
					tcpListener.ReceiveDataAsync(clientId, TcpClientReceiveHandler);
				}
			}

			UpdateListeningState();
		}

		private static SocketStateEventArgs.eSocketStatus GetSocketStatus(SocketStatus status)
		{
			switch (status)
			{
				case SocketStatus.SOCKET_STATUS_NO_CONNECT:
					return SocketStateEventArgs.eSocketStatus.SocketStatusNoConnect;
				case SocketStatus.SOCKET_STATUS_WAITING:
					return SocketStateEventArgs.eSocketStatus.SocketStatusWaiting;
				case SocketStatus.SOCKET_STATUS_CONNECTED:
					return SocketStateEventArgs.eSocketStatus.SocketStatusConnected;
				case SocketStatus.SOCKET_STATUS_CONNECT_FAILED:
					return SocketStateEventArgs.eSocketStatus.SocketStatusConnectFailed;
				case SocketStatus.SOCKET_STATUS_BROKEN_REMOTELY:
					return SocketStateEventArgs.eSocketStatus.SocketStatusBrokenRemotely;
				case SocketStatus.SOCKET_STATUS_BROKEN_LOCALLY:
					return SocketStateEventArgs.eSocketStatus.SocketStatusBrokenLocally;
				case SocketStatus.SOCKET_STATUS_DNS_LOOKUP:
					return SocketStateEventArgs.eSocketStatus.SocketStatusDnsLookup;
				case SocketStatus.SOCKET_STATUS_DNS_FAILED:
					return SocketStateEventArgs.eSocketStatus.SocketStatusDnsFailed;
				case SocketStatus.SOCKET_STATUS_DNS_RESOLVED:
					return SocketStateEventArgs.eSocketStatus.SocketStatusDnsResolved;
				case SocketStatus.SOCKET_STATUS_LINK_LOST:
					return SocketStateEventArgs.eSocketStatus.SocketStatusLinkLost;
				case SocketStatus.SOCKET_STATUS_SOCKET_NOT_EXIST:
					return SocketStateEventArgs.eSocketStatus.SocketStatusSocketNotExist;
				default:
					throw new ArgumentOutOfRangeException("status");
			}
		}

		/// <summary>
		/// Handles an incoming TCP connection
		/// </summary>
		/// <param name="tcpListener"></param>
		/// <param name="clientId"></param>
		private void TcpClientConnectCallback(TCPServer tcpListener, uint clientId)
		{
			// Spawn new thread for accepting new clients
			tcpListener.WaitForConnectionAsync(AddressToAcceptConnectionFrom, TcpClientConnectCallback);
		}

		/// <summary>
		/// Handles receiving data from a specific client
		/// </summary>
		/// <param name="tcpListener"></param>
		/// <param name="clientId"></param>
		/// <param name="bytesReceived"></param>
		private void TcpClientReceiveHandler(TCPServer tcpListener, uint clientId, int bytesReceived)
		{
			if (clientId == 0)
				return;

			byte[] buffer = tcpListener.GetIncomingDataBufferForSpecificClient(clientId);

			// Buffer is null if there is no client with the given id connected
			if (buffer == null)
			{
				RemoveClient(clientId, SocketStateEventArgs.eSocketStatus.SocketStatusNoConnect);
				return;
			}

			TcpReceiveEventArgs eventArgs = new TcpReceiveEventArgs(clientId, buffer, bytesReceived);
			HostInfo hostInfo = GetClientInfo(clientId);

			PrintRx(hostInfo, eventArgs.Data);
			RaiseOnDataReceived(eventArgs);

			// Would this ever happen?
			if (!ClientConnected(clientId))
			{
				RemoveClient(clientId, SocketStateEventArgs.eSocketStatus.SocketStatusNoConnect);
				return;
			}

			// Spawn a new listening thread
			SocketErrorCodes socketError = tcpListener.ReceiveDataAsync(clientId, TcpClientReceiveHandler);
			UpdateListeningState();

			if (socketError == SocketErrorCodes.SOCKET_OPERATION_PENDING)
				return;

			Logger.AddEntry(eSeverity.Error,
			                "{0} - ClientId {1} hostname {2} failed to ReceiveDataAsync: {3}",
			                this, clientId, GetHostInfoForClientId(clientId), socketError);

			RemoveClient(clientId, SocketStateEventArgs.eSocketStatus.SocketStatusNoConnect);
		}

		/// <summary>
		/// Gets the hostname for the client in the format 0.0.0.0:0
		/// </summary>
		/// <param name="clientId"></param>
		/// <returns></returns>
		public HostInfo GetHostInfoForClientId(uint clientId)
		{
			if (m_TcpListener == null)
				throw new InvalidOperationException("Server is not connected");

			string host = m_TcpListener.GetAddressServerAcceptedConnectionFromForSpecificClient(clientId);
			ushort port = (ushort)m_TcpListener.GetPortNumberServerAcceptedConnectionFromForSpecificClient(clientId);

			return new HostInfo(host, port);
		}

		private void UpdateListeningState()
		{
			Listening = m_TcpListener != null && m_TcpListener.State > ServerState.SERVER_NOT_LISTENING;
		}

		#endregion
	}
}

#endif
