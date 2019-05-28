#if SIMPLSHARP
using System;
using Crestron.SimplSharp;
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
			m_ConnectionLock.Enter();

			try
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

				Logger.AddEntry(eSeverity.Notice, string.Format("{0} - Listening on port {1} with max # of connections {2}", this, Port,
																MaxNumberOfClients));
			}
			finally
			{
				m_ConnectionLock.Leave();
			}
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
			m_ConnectionLock.Enter();

			try
			{
				if (disable)
				{
					Logger.AddEntry(eSeverity.Notice, "{0} - Stopping server", this);
					Enabled = false;
				}
				else
				{
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
						if (!e.Message.Contains("Object not initialized"))
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
					RemoveClient(client);
			}
			finally
			{
				m_ConnectionLock.Leave();
			}
		}

		/// <summary>
		/// Sends the data to all connected clients.
		/// </summary>
		/// <param name="data"></param>
		public void Send(string data)
		{
			m_ConnectionLock.Enter();

			try
			{
				if (m_Connections.Count == 0)
					return;

				byte[] byteData = StringUtils.ToBytes(data);

				foreach (uint clientId in m_Connections.Keys)
				{
					PrintTx(clientId, data);
					m_TcpListener.SendDataAsync(clientId, byteData, byteData.Length, (tcpListener, clientIndex, bytesCount) => { });
				}
			}
			finally
			{
				m_ConnectionLock.Leave();
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
			m_ConnectionLock.Enter();

			try
			{
				if (!ClientConnected(clientId))
				{
					Logger.AddEntry(eSeverity.Notice, "{0} - Unable to send data to unconnected client {1}", this, clientId);
					RemoveClient(clientId);
					return;
				}

				byte[] byteData = StringUtils.ToBytes(data);

				PrintTx(clientId, data);
				m_TcpListener.SendDataAsync(clientId, byteData, byteData.Length, (tcpListener, clientIndex, bytesCount) => { });
			}
			finally
			{
				m_ConnectionLock.Leave();
			}
		}

		/// <summary>
		/// Gets the address and port for the client with the given id.
		/// </summary>
		/// <param name="client"></param>
		/// <returns></returns>
		[PublicAPI]
		public HostInfo GetClientInfo(uint client)
		{
			m_ConnectionLock.Enter();

			try
			{
				string address = m_TcpListener.GetAddressServerAcceptedConnectionFromForSpecificClient(client);
				ushort port = (ushort)m_TcpListener.GetPortNumberServerAcceptedConnectionFromForSpecificClient(client);

				return new HostInfo(address, port);
			}
			finally
			{
				m_ConnectionLock.Leave();
			}
		}

		/// <summary>
		/// Returns true if the client is connected.
		/// </summary>
		/// <param name="client"></param>
		/// <returns></returns>
		public bool ClientConnected(uint client)
		{
			m_ConnectionLock.Enter();

			try
			{
				// This is a hack. We have no way of determining if a client id is still valid,
				// so if we get a null address we know the client is invalid.
				if (m_TcpListener.GetLocalAddressServerAcceptedConnectionFromForSpecificClient(client) == null)
					return false;

				return m_TcpListener.ClientConnected(client);
			}
			finally
			{
				m_ConnectionLock.Leave();
			}
		}

		/// <summary>
		/// Fires on a change in socket status
		/// </summary>
		/// <param name="tcpListener"></param>
		/// <param name="clientId"></param>
		/// <param name="status"></param>
		private void HandleSocketStatusChange(TCPServer tcpListener, uint clientId, SocketStatus status)
		{
			m_ConnectionLock.Enter();

			try
			{
				if (clientId != 0)
				{
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

				UpdateListeningState();
			}
			finally
			{
				m_ConnectionLock.Leave();
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
			m_ConnectionLock.Enter();

			try
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

					TcpReceiveEventArgs eventArgs = new TcpReceiveEventArgs(clientId, buffer, bytesReceived);
					PrintRx(clientId, eventArgs.Data);

					OnDataReceived.Raise(null, eventArgs);

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
				UpdateListeningState();

				if (socketError == SocketErrorCodes.SOCKET_OPERATION_PENDING)
					return;

				Logger.AddEntry(eSeverity.Error,
								"{0} - ClientId {1} hostname {2} failed to ReceiveDataAsync: {3}",
								this, clientId, GetHostnameForClientId(clientId), socketError);

				RemoveClient(clientId);
			}
			finally
			{
				m_ConnectionLock.Leave();
			}
		}

		/// <summary>
		/// Gets the hostname for the client in the format 0.0.0.0:0
		/// </summary>
		/// <param name="clientId"></param>
		/// <returns></returns>
		private string GetHostnameForClientId(uint clientId)
		{
			m_ConnectionLock.Enter();

			try
			{
				return string.Format("{0}:{1}",
					 m_TcpListener.GetAddressServerAcceptedConnectionFromForSpecificClient(clientId),
					 m_TcpListener.GetPortNumberServerAcceptedConnectionFromForSpecificClient(clientId));
			}
			finally
			{
				m_ConnectionLock.Leave();
			}
		}

		private void UpdateListeningState()
		{
			m_ConnectionLock.Enter();

			try
			{
				Listening = m_TcpListener != null && m_TcpListener.State > ServerState.SERVER_NOT_LISTENING;
			}
			finally
			{
				m_ConnectionLock.Leave();
			}
		}
	}
}

#endif
