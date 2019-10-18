﻿#if STANDARD
using ICD.Common.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using ICD.Common.Properties;
using ICD.Connect.Protocol.Ports;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Settings.Utils;

namespace ICD.Connect.Protocol.Network.Ports.Tcp
{
	public sealed partial class IcdTcpServer
	{
		private TcpListenerEx m_TcpListener;

		private readonly Dictionary<uint, TcpClient> m_TcpClients = new Dictionary<uint, TcpClient>();
		private readonly Dictionary<uint, byte[]> m_ClientBuffers = new Dictionary<uint, byte[]>();

		/// <summary>
		/// Starts the TCP Server
		/// </summary>
		[PublicAPI]
		public void Start()
		{
			try
			{
				m_TcpListener = new TcpListenerEx(IPAddress.Any, Port);
				m_TcpListener.Server.ReceiveBufferSize = BufferSize;

				try
				{
					m_TcpListener.Start();
				}
				catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
				{
					// if application crashes on linux it doesn't clean up the socket use immediately
					m_TcpListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
					m_TcpListener.Start();
				}
				// if more socket exceptions start popping up, take a look here for different error codes
				// https://docs.microsoft.com/en-us/windows/desktop/WinSock/windows-sockets-error-codes-2

				m_TcpListener.AcceptTcpClientAsync().ContinueWith(TcpClientConnectCallback);

				Enabled = true;

				Logger.AddEntry(eSeverity.Notice, string.Format("{0} - Listening on port {1} with max # of connections {2}", this,
				                                                Port, MaxNumberOfClients));
			}
			catch (Exception e)
			{
				m_TcpListener = null;

				Logger.AddEntry(eSeverity.Error, string.Format("{0} - Failed to start listening - {1}", this, e.Message));
			}
			finally
			{
				UpdateListeningState();
			}
		}

		/// <summary>
		/// Stops and Disables the TCP Server
		/// </summary>
		[PublicAPI]
		public void Stop()
		{
			Stop(true);
		}

		/// <summary>
		/// Stops the TCP server.
		/// </summary>
		/// <param name="disable">When true disables the TCP server.</param>
		[PublicAPI]
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
				m_TcpListener.Stop();

				IPEndPoint endpoint = m_TcpListener.LocalEndpoint as IPEndPoint;
				if (endpoint == null)
					Logger.AddEntry(eSeverity.Notice, "{0} - No longer listening", this);
				else
					Logger.AddEntry(eSeverity.Notice, "{0} - No longer listening on port {1}", this, endpoint.Port);
			}

			m_TcpListener = null;

			foreach (uint client in GetClients())
				RemoveTcpClient(client);

			UpdateListeningState();
		}

		/// <summary>
		/// Sends the data to all connected clients.
		/// </summary>
		/// <param name="data"></param>
		public void Send(string data)
		{
			byte[] byteData = StringUtils.ToBytes(data);

			foreach (uint clientId in GetClients())
			{
				HostInfo hostInfo = GetHostInfoForClientId(clientId);
				PrintTx(hostInfo, data);
				Send(clientId, byteData);
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
			byte[] byteData = StringUtils.ToBytes(data);
			HostInfo hostInfo = GetHostInfoForClientId(clientId);

			PrintTx(hostInfo, data);
			Send(clientId, byteData);
		}

		public void Send(uint clientId, byte[] data)
		{
			if (!ClientConnected(clientId))
			{
				Logger.AddEntry(eSeverity.Warning, "{0} - Unable to send data to unconnected client {1}", this,
				                clientId);
				return;
			}

			try
			{
				m_ClientsSection.Execute(() => m_TcpClients[clientId])
				                .Client.Send(data, 0, data.Length, SocketFlags.None);
			}
			catch (SocketException ex)
			{
				Logger.AddEntry(eSeverity.Error, ex, "Failed to send data to client {0}",
				                GetHostInfoForClientId(clientId));
			}

			if (!ClientConnected(clientId))
				RemoveTcpClient(clientId);
		}

		/// <summary>
		/// Gets the address and port for the client with the given id.
		/// </summary>
		/// <param name="clientId"></param>
		/// <returns></returns>
		[PublicAPI]
		public HostInfo GetHostInfoForClientId(uint clientId)
		{
			TcpClient tcpClient = m_ClientsSection.Execute(() => m_TcpClients[clientId]);
			IPEndPoint endpoint = tcpClient.Client.RemoteEndPoint as IPEndPoint;

			return endpoint == null ? new HostInfo() : new HostInfo(endpoint.Address.ToString(), (ushort)endpoint.Port);
		}

		/// <summary>
		/// Returns true if the client is connected.
		/// </summary>
		/// <param name="client"></param>
		/// <returns></returns>
		public bool ClientConnected(uint client)
		{
			m_ClientsSection.Enter();

			try
			{
				// This is a hack. We have no way of determining if a client id is still valid,
				// so if we get a null address we know the client is invalid.
				TcpClient tcpClient;
				if (!m_TcpClients.TryGetValue(client, out tcpClient) || tcpClient == null)
					return false;

				return tcpClient.Connected;
			}
			finally
			{
				m_ClientsSection.Leave();
			}
		}

		/// <summary>
		/// Handles an incoming TCP connection
		/// </summary>
		/// <param name="task"></param>
		private void TcpClientConnectCallback(Task<TcpClient> task)
		{
			TcpClient client;
			uint clientId;

			m_ClientsSection.Enter();

			try
			{
				clientId = (uint)IdUtils.GetNewId(m_Clients.Keys.Select(i => (int)i));
				if (task.Status == TaskStatus.Faulted)
					return;

				client = task.Result;

				m_TcpClients[clientId] = client;
				m_ClientBuffers[clientId] = new byte[16384];
			}
			finally
			{
				m_ClientsSection.Leave();
			}

			client.GetStream()
			      .ReadAsync(m_ClientBuffers[clientId], 0, 16384)
			      .ContinueWith(a => TcpClientReceiveHandler(a, clientId));

			// Spawn new thread for accepting new clients
			m_TcpListener.AcceptTcpClientAsync().ContinueWith(TcpClientConnectCallback);

			// let the rest of the application know a new client connected
			AddClient(clientId, SocketStateEventArgs.eSocketStatus.SocketStatusConnected);

			UpdateListeningState();
		}

		/// <summary>
		/// Handles receiving data from a specific client
		/// </summary>
		/// <param name="task"></param>
		/// <param name="clientId"></param>
		private void TcpClientReceiveHandler(Task<int> task, uint clientId)
		{
			if (clientId == 0)
				return;

			byte[] buffer = m_ClientBuffers[clientId];

			int length = 0;

			try
			{
				length = task.Result;
			}
			catch (AggregateException ae)
			{
				ae.Handle(e =>
				{
					// Aborted by local software
					if (e is IOException)
						return true;

					// Aborted by remote host
					if (e is SocketException)
						return true;

					return false;
				});
			}
			if (length <= 0)
			{
				RemoveTcpClient(clientId);
				return;
			}

			TcpReceiveEventArgs eventArgs = new TcpReceiveEventArgs(clientId, buffer, length);
			HostInfo hostInfo = GetHostInfoForClientId(clientId);

			PrintRx(hostInfo, eventArgs.Data);
			RaiseOnDataReceived(eventArgs);

			if (!ClientConnected(clientId))
			{
				RemoveTcpClient(clientId);
				return;
			}

			// Spawn a new listening thread
			m_ClientsSection.Execute(() => m_TcpClients[clientId])
			                .GetStream()
			                .ReadAsync(buffer, 0, 16384)
			                .ContinueWith(a => TcpClientReceiveHandler(a, clientId));

			UpdateListeningState();
		}

		private void RemoveTcpClient(uint clientId)
		{
			m_ClientsSection.Enter();

			try
			{
				TcpClient client;
				if (m_TcpClients.TryGetValue(clientId, out client))
				{
					m_TcpClients.Remove(clientId);
					client.Dispose();
				}
			}
			finally
			{
				m_ClientsSection.Leave();
			}

			RemoveClient(clientId, SocketStateEventArgs.eSocketStatus.SocketStatusNoConnect);
		}

		private void UpdateListeningState()
		{
			Listening = m_TcpListener != null && m_TcpListener.Active;
		}
	}

	public sealed class TcpListenerEx : TcpListener
	{
		public new bool Active
		{
			get { return base.Active; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:System.Net.Sockets.TcpListener"/> class with the specified local endpoint.
		/// </summary>
		/// <param name="localEP">An <see cref="T:System.Net.IPEndPoint"/> that represents the local endpoint to which to bind the listener <see cref="T:System.Net.Sockets.Socket"/>. </param><exception cref="T:System.ArgumentNullException"><paramref name="localEP"/> is null. </exception>
		public TcpListenerEx(IPEndPoint localEP)
			: base(localEP)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:System.Net.Sockets.TcpListener"/> class that listens for incoming connection attempts on the specified local IP address and port number.
		/// </summary>
		/// <param name="localaddr">An <see cref="T:System.Net.IPAddress"/> that represents the local IP address. </param><param name="port">The port on which to listen for incoming connection attempts. </param><exception cref="T:System.ArgumentNullException"><paramref name="localaddr"/> is null. </exception><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="port"/> is not between <see cref="F:System.Net.IPEndPoint.MinPort"/> and <see cref="F:System.Net.IPEndPoint.MaxPort"/>. </exception>
		public TcpListenerEx(IPAddress localaddr, int port)
			: base(localaddr, port)
		{
		}
	}
}
#endif
