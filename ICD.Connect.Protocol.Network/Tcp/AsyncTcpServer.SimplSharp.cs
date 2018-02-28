#if SIMPLSHARP
using System;
using Crestron.SimplSharp.CrestronSockets;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.Network.Tcp
{
	public sealed partial class AsyncTcpServer
	{
		private TCPServer m_TcpListener;

		/// <summary>
		/// Starts the TCP Server
		/// </summary>
		[PublicAPI]
		public void Start()
		{
			Active = true;

			m_TcpListener = new TCPServer(Port, MaxNumberOfClients);
			m_TcpListener.SocketStatusChange += HandleSocketStatusChange;
			m_TcpListener.WaitForConnectionAsync(AddressToAcceptConnectionFrom, TcpClientConnectCallback);

			Logger.AddEntry(eSeverity.Notice, string.Format("{0} - Listening on port {1} with max # of connections {2}", this, Port,
			                                                MaxNumberOfClients));
		}

		/// <summary>
		/// Stops and Disables the TCP Server
		/// </summary>
		public void Stop()
		{
			Active = false;

			if (m_TcpListener != null)
			{
				m_TcpListener.SocketStatusChange -= HandleSocketStatusChange;

				m_TcpListener.Stop();
				m_TcpListener.DisconnectAll();

				Logger.AddEntry(eSeverity.Notice, "{0} - No longer listening on port {1}", this, m_TcpListener.PortNumber);
			}
			m_TcpListener = null;

			foreach (uint client in GetClients())
				RemoveClient(client);
		}

		/// <summary>
		/// Sends the data to all connected clients.
		/// </summary>
		/// <param name="data"></param>
		public void Send(string data)
		{
			byte[] byteData = StringUtils.ToBytes(data);
			
			foreach (uint clientId in GetClients())
				m_TcpListener.SendDataAsync(clientId, byteData, byteData.Length, (tcpListener, clientIndex, bytesCount) => { });
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
				Logger.AddEntry(eSeverity.Notice, "{0} unable to send data to unconnected client {1}", GetType().Name, clientId);
				RemoveClient(clientId);
				return;
			}

			byte[] byteData = StringUtils.ToBytes(data);
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
			// This is a hack. We have no way of determining if a client id is still valid,
			// so if we get a null address we know the client is invalid.
			if (m_TcpListener.GetLocalAddressServerAcceptedConnectionFromForSpecificClient(client) == null)
				return false;

			return m_TcpListener.ClientConnected(client);
		}

		/// <summary>
		/// Fires on a change in socket status
		/// </summary>
		/// <param name="tcpListener"></param>
		/// <param name="clientId"></param>
		/// <param name="status"></param>
		private void HandleSocketStatusChange(TCPServer tcpListener, uint clientId, SocketStatus status)
		{
			if (clientId == 0)
				return;

			// Client disconnected
			if (!ClientConnected(clientId))
			{
				RemoveClient(clientId);
			}
			// Client connected
			else if (!ContainsClient(clientId))
			{
				AddClient(clientId);
				tcpListener.ReceiveDataAsync(clientId, TcpClientReceiveHandler);
			}

			try
			{
				SocketStateEventArgs.eSocketStatus socketStatus = GetSocketStatus(status);
				OnSocketStateChange.Raise(this, new SocketStateEventArgs(socketStatus, clientId));
			}
			catch (Exception e)
			{
				Logger.AddEntry(eSeverity.Error, e, "{0} - Exception in OnSocketStateChange callback - {1}", this, e.Message);
			}
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
			try
			{
				if (clientId == 0)
					return;

				byte[] buffer = tcpListener.GetIncomingDataBufferForSpecificClient(clientId);

				// Buffer is null if there is no client with the given id connected
				if (buffer == null)
				{
					RemoveClient(clientId);
					return;
				}

				OnDataReceived.Raise(null, new TcpReceiveEventArgs(clientId, buffer, bytesReceived));

				// Would this ever happen?
				if (!ClientConnected(clientId))
				{
					RemoveClient(clientId);
					return;
				}
			}
			catch (Exception e)
			{
				Logger.AddEntry(eSeverity.Error, e, "{0} - Exception occurred while processing received data", this);
			}

			// Spawn a new listening thread
			SocketErrorCodes socketError = tcpListener.ReceiveDataAsync(clientId, TcpClientReceiveHandler);
			if (socketError == SocketErrorCodes.SOCKET_OPERATION_PENDING)
				return;

			Logger.AddEntry(eSeverity.Error,
			                "{0} - ClientId {1} hostname {2} failed to ReceiveDataAsync: {3}",
			                this, clientId, GetHostnameForClientId(clientId), socketError);

			RemoveClient(clientId);
		}

		/// <summary>
		/// Gets the hostname for the client in the format 0.0.0.0:0
		/// </summary>
		/// <param name="clientId"></param>
		/// <returns></returns>
		private string GetHostnameForClientId(uint clientId)
		{
			return string.Format("{0}:{1}",
			                     m_TcpListener.GetAddressServerAcceptedConnectionFromForSpecificClient(clientId),
			                     m_TcpListener.GetPortNumberServerAcceptedConnectionFromForSpecificClient(clientId));
		}
	}
}

#endif
