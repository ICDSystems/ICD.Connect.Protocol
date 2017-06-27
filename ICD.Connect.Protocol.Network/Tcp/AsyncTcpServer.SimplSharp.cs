#if SIMPLSHARP
using System;
using Crestron.SimplSharp.CrestronSockets;
using ICD.Common.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Services;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
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

			ServiceProvider.TryGetService<ILoggerService>()
			               .AddEntry(eSeverity.Notice,
			                         string.Format("AsyncTcpServer Listening on Port {0} with Max # of Connections {1}", Port,
			                                       MaxNumberOfClients));
		}

		/// <summary>
		/// Stops and Disables the TCP Server
		/// </summary>
		/// <param name="retainEnabledState">Should the TcpListener be enabled, normally false, true for link state handling</param>
		[PublicAPI]
		public void Stop(bool retainEnabledState)
		{
			Active = retainEnabledState;

			if (m_TcpListener != null)
			{
				m_TcpListener.DisconnectAll();
				m_TcpListener.SocketStatusChange -= HandleSocketStatusChange;
				ServiceProvider.TryGetService<ILoggerService>()
				               .AddEntry(eSeverity.Notice,
				                         string.Format("AsyncTcpServer No Longer Listening on Port {0}", m_TcpListener.PortNumber));
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
			m_TcpListener.SendDataAsync(byteData, byteData.Length, (tcpListener, clientIndex, bytesCount) => { });
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
				string message = string.Format("{0} unable to send data to unconnected client {1}", GetType().Name, clientId);
				throw new InvalidOperationException(message);
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
				SocketStateEventArgs.eSocketStatus socketStatus = SocketStateEventArgs.GetSocketStatus(status);
				OnSocketStateChange.Raise(this, new SocketStateEventArgs(socketStatus, clientId));
			}
			catch (Exception e)
			{
				ServiceProvider.TryGetService<ILoggerService>()
				               .AddEntry(eSeverity.Error, e, "Exception in OnSocketStateChange callback - {0}", e.Message);
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

			OnDataReceived.Raise(null, new TcpReceiveEventArgs(clientId, buffer, bytesReceived));

			if (!ClientConnected(clientId))
			{
				RemoveClient(clientId);
				return;
			}

			// Spawn a new listening thread
			SocketErrorCodes socketError = tcpListener.ReceiveDataAsync(clientId, TcpClientReceiveHandler);
			if (socketError == SocketErrorCodes.SOCKET_OPERATION_PENDING)
				return;

			ServiceProvider.TryGetService<ILoggerService>()
			               .AddEntry(eSeverity.Error,
			                         "AsyncTcpServer[ClientId({0}) RemoteClient({1})] failed to ReceiveDataAsync: {2}",
			                         clientId, GetHostnameForClientId(clientId), socketError);

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
