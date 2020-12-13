#if SIMPLSHARP
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Network.Servers;
using System;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.Network.Ports.TcpSecure
{
	public sealed partial class IcdSecureTcpServer
	{
		[CanBeNull]
		private SecureTCPServer m_TcpListener;

		#region Methods

		/// <summary>
		/// Starts the TCP Server
		/// </summary>
		[PublicAPI]
		public override void Start()
		{
			if (m_TcpListener != null)
				throw new InvalidOperationException("Secure TCP Server must be stopped before it is started again.");

			if (!IcdEnvironment.SslEnabled)
				throw new InvalidOperationException("ControlSystem must have SSL enabled before starting SecureTCPServer");

			Logger.Log(eSeverity.Notice, "Starting server");

			Enabled = true;

			m_TcpListener = new SecureTCPServer(AddressToAcceptConnectionFrom, Port, BufferSize,
										  EthernetAdapterType.EthernetUnknownAdapter, MaxNumberOfClients);
			m_TcpListener.SocketStatusChange += HandleSocketStatusChange;
			m_TcpListener.WaitForConnectionsAlways(TcpClientConnectCallback);

			// Hack - I think it takes a little while for the listening state to update
			Listening = true;

			Logger.Log(eSeverity.Notice,
							string.Format("Listening on port {0} with max # of connections {1}", Port,
										  MaxNumberOfClients));
		}

		/// <summary>
		/// Stops the TCP server.
		/// </summary>
		/// <param name="disable">When true disables the TCP server.</param>
		protected override void Stop(bool disable)
		{
			if (disable)
			{
				if (m_TcpListener != null)
					Logger.Log(eSeverity.Notice, "Stopping server");
				Enabled = false;
			}
			else
			{
				if (m_TcpListener != null)
					Logger.Log(eSeverity.Notice, "Temporarily stopping server");
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
					Logger.Log(eSeverity.Notice, "No longer listening on port {0}", m_TcpListener.PortNumber);

					m_TcpListener = null;
					UpdateListeningState();
				}
			}

			foreach (uint client in GetClients())
				RemoveClient(client, SocketStateEventArgs.eSocketStatus.SocketStatusNoConnect);
		}

		protected override void SendWorkerAction(uint clientId, string data)
		{
			if (m_TcpListener == null)
				throw new InvalidOperationException("Cannot Send Worker Action with no TcpListener");

			if (!ClientConnected(clientId))
			{
				Logger.Log(eSeverity.Warning, "Unable to send data to unconnected client {0}", clientId);
				RemoveClient(clientId, SocketStateEventArgs.eSocketStatus.SocketStatusNoConnect);
				return;
			}

			byte[] byteData = StringUtils.ToBytes(data);
			HostInfo hostInfo = GetClientInfo(clientId);

			PrintTx(hostInfo, data);
			SocketErrorCodes response = m_TcpListener.SendData(clientId, byteData, byteData.Length);
			if (response != SocketErrorCodes.SOCKET_OK)
				Logger.Log(eSeverity.Error, "Error sending data to {0}: {1}", hostInfo, response);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Fires on a change in socket status
		/// </summary>
		/// <param name="tcpListener"></param>
		/// <param name="clientId"></param>
		/// <param name="status"></param>
		private void HandleSocketStatusChange(SecureTCPServer tcpListener, uint clientId, SocketStatus status)
		{
			// Spawn a new thread to handle status changes
			// Mitigration strategy for Creston TCPServer bugs
			ThreadingUtils.SafeInvoke(() => SocketStatusChangeWorker(tcpListener, clientId, status));
		}

		/// <summary>
		/// Fires on a change in socket status
		/// </summary>
		/// <param name="tcpListener"></param>
		/// <param name="clientId"></param>
		/// <param name="status"></param>
		private void SocketStatusChangeWorker(SecureTCPServer tcpListener, uint clientId, SocketStatus status)
		{
			SocketStateEventArgs.eSocketStatus reason = GetSocketStatus(status);

			try
			{
				if (clientId == 0)
					return;

				// Client disconnected
				if (!tcpListener.ClientConnected(clientId))
				{
					RemoveClient(clientId, reason);
				}
				// Client connected
				else if (!ContainsClient(clientId))
				{
					string host = tcpListener.GetAddressServerAcceptedConnectionFromForSpecificClient(clientId);
					ushort port = (ushort)tcpListener.GetPortNumberServerAcceptedConnectionFromForSpecificClient(clientId);
					HostInfo clientInfo = new HostInfo(host, port);

					AddClient(clientId, clientInfo, reason);
					tcpListener.ReceiveDataAsync(clientId, TcpClientReceiveHandler);
				}
			}
			finally
			{
				UpdateListeningState();
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
		private void TcpClientConnectCallback(SecureTCPServer tcpListener, uint clientId)
		{
			// Spawn new thread for accepting new clients
			if (tcpListener.NumberOfClientsConnected >= tcpListener.MaxNumberOfClientSupported)
				Logger.Log(eSeverity.Warning, "{0} - Max number of clients reached:{1}", this,
						   tcpListener.MaxNumberOfClientSupported);
		}

		/// <summary>
		/// Handles receiving data from a specific client
		/// </summary>
		/// <param name="tcpListener"></param>
		/// <param name="clientId"></param>
		/// <param name="bytesReceived"></param>
		private void TcpClientReceiveHandler(SecureTCPServer tcpListener, uint clientId, int bytesReceived)
		{
			if (clientId == 0)
				return;

			// If bytesReceived is <= 0, client disconnected
			if (bytesReceived <= 0)
			{
				RemoveClient(clientId, SocketStateEventArgs.eSocketStatus.SocketStatusNoConnect);
				return;
			}

			byte[] buffer = tcpListener.GetIncomingDataBufferForSpecificClient(clientId);

			// Buffer is null if there is no client with the given id connected
			if (buffer == null)
			{
				RemoveClient(clientId, SocketStateEventArgs.eSocketStatus.SocketStatusNoConnect);
				return;
			}

			DataReceiveEventArgs eventArgs = new DataReceiveEventArgs(clientId, buffer, bytesReceived);
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

			Logger.Log(eSeverity.Error,
							"Failed to receive data from ClientId {0} at {1} : {2}",
							clientId, GetClientInfo(clientId), socketError);

			RemoveClient(clientId, SocketStateEventArgs.eSocketStatus.SocketStatusNoConnect);
		}

		private void UpdateListeningState()
		{
			Listening = m_TcpListener != null && m_TcpListener.State > ServerState.SERVER_NOT_LISTENING;

			// If we are enabled but not listening, probably hit max clients - check to see if we're not and reset listener
			if (m_TcpListener != null && Enabled && !Listening &&
				m_TcpListener.NumberOfClientsConnected < m_TcpListener.MaxNumberOfClientSupported)
			{
				m_TcpListener.WaitForConnectionsAlways(TcpClientConnectCallback);
			}
		}

		#endregion

		#region console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			if (m_TcpListener != null)
			{
				addRow("Listener Clients", m_TcpListener.NumberOfClientsConnected);
				addRow("Listener Pending", m_TcpListener.Pending());
			}
		}

		#endregion
	}
}
#endif
