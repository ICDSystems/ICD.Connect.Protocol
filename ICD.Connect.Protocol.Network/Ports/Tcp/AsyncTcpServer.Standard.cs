#if STANDARD
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
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Settings;

namespace ICD.Connect.Protocol.Network.Ports.Tcp
{
	public sealed partial class AsyncTcpServer
	{
		private TcpListener m_TcpListener;

		private readonly Dictionary<uint, TcpClient> m_Clients = new Dictionary<uint, TcpClient>();
		private readonly SafeCriticalSection m_ClientsSection = new SafeCriticalSection();
		private readonly Dictionary<uint, byte[]> m_ClientBuffers = new Dictionary<uint, byte[]>();

		/// <summary>
		/// Starts the TCP Server
		/// </summary>
		[PublicAPI]
		public void Start()
		{
			Active = true;

			m_TcpListener = new TcpListener(IPAddress.Any, Port);
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

			Logger.AddEntry(eSeverity.Notice, string.Format("{0} - Listening on port {1} with max # of connections {2}", this, Port,
			                                                MaxNumberOfClients));
		}

		/// <summary>
		/// Stops and Disables the TCP Server
		/// </summary>
		[PublicAPI]
		public void Stop()
		{
			Active = false;

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
				PrintTx(clientId, data);
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

			PrintTx(clientId, data);
			Send(clientId, byteData);
		}

		public void Send(uint clientId, byte[] data)
		{
			if (!ClientConnected(clientId))
			{
				Logger.AddEntry(eSeverity.Warning, "{0} - Unable to send data to unconnected client {1}", this, clientId);
				return;
			}

			try
			{
				m_Clients[clientId].Client.Send(data, 0, data.Length, SocketFlags.None);
			}
			catch (SocketException ex)
			{
				Logger.AddEntry(eSeverity.Error, ex, "Failed to send data to client {0}", GetHostnameForClientId(clientId));
			}

			if (!ClientConnected(clientId))
			{
				RemoveTcpClient(clientId);
			}
		}

		/// <summary>
		/// Gets the address and port for the client with the given id.
		/// </summary>
		/// <param name="clientId"></param>
		/// <returns></returns>
		[PublicAPI]
		public HostInfo GetClientInfo(uint clientId)
		{
			TcpClient client = m_Clients[clientId];
			IPEndPoint endpoint = client.Client.RemoteEndPoint as IPEndPoint;

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
				if (!m_Clients.ContainsKey(client) || m_Clients[client] == null)
					return false;

				return m_Clients[client].Connected;
			}
			finally
			{
				m_ClientsSection.Leave();
			}
		}

		/// <summary>
		/// Handles an incoming TCP connection
		/// </summary>
		/// <param name="tcpClient"></param>
		private void TcpClientConnectCallback(Task<TcpClient> tcpClient)
		{
			uint clientId = 0;
			m_ClientsSection.Enter();
			try
			{
				clientId = (uint) IdUtils.GetNewId(m_Clients.Keys.Select(i => (int) i));
				if (tcpClient.Status == TaskStatus.Faulted || clientId == 0)
				{
					return;
				}

				m_Clients[clientId] = tcpClient.Result;
				m_ClientBuffers[clientId] = new byte[16384];
				AddClient(clientId);
				m_Clients[clientId].GetStream()
					.ReadAsync(m_ClientBuffers[clientId], 0, 16384)
					.ContinueWith(a => TcpClientReceiveHandler(a, clientId));
			}
			finally
			{
				m_ClientsSection.Leave();
			}

			// Spawn new thread for accepting new clients
			m_TcpListener.AcceptTcpClientAsync().ContinueWith(TcpClientConnectCallback);

			// let the rest of the application know a new client connected
			if(clientId != 0)
				OnSocketStateChange.Raise(this,
					new SocketStateEventArgs(SocketStateEventArgs.eSocketStatus.SocketStatusConnected, clientId));
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

			if (length > 0)
			{
				TcpReceiveEventArgs eventArgs = new TcpReceiveEventArgs(clientId, buffer, length);

				PrintRx(clientId, eventArgs.Data);
				OnDataReceived.Raise(null, eventArgs);
			}

			if (!ClientConnected(clientId))
			{
				RemoveTcpClient(clientId);
				
				return;
			}

			// Spawn a new listening thread
			m_Clients[clientId].GetStream()
			                   .ReadAsync(buffer, 0, 16384)
			                   .ContinueWith(a => TcpClientReceiveHandler(a, clientId));
		}

		private void RemoveTcpClient(uint clientId)
		{
			m_ClientsSection.Enter();

			try
			{
				if (m_Clients.ContainsKey(clientId))
				{
					var client = m_Clients[clientId];
					m_Clients.Remove(clientId);
					client?.Dispose();
				}

				RemoveClient(clientId);
			}
			finally
			{
				m_ClientsSection.Leave();
			}
		}

		/// <summary>
		/// Gets the hostname for the client in the format 0.0.0.0:0
		/// </summary>
		/// <param name="clientId"></param>
		/// <returns></returns>
		private string GetHostnameForClientId(uint clientId)
		{
			return GetClientInfo(clientId).ToString();
		}
	}
}
#endif
