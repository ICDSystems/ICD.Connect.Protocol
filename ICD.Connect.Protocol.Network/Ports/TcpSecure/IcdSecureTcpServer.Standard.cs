#if STANDARD
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using ICD.Common.Logging.LoggingContexts;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Network.Ports.Tcp;
using ICD.Connect.Protocol.Network.Servers;
using ICD.Connect.Protocol.Network.Utils;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Utils;

namespace ICD.Connect.Protocol.Network.Ports.TcpSecure
{
	public sealed partial class IcdSecureTcpServer
	{
		private X509Certificate m_ServerCertificate;

		private TcpListenerEx m_TcpListener;

		private readonly Dictionary<uint, TcpClient> m_TcpClients;
		private readonly Dictionary<uint, byte[]> m_ClientBuffers;
		private readonly SafeCriticalSection m_ClientsSection;

		/// <summary>
		/// Constructor.
		/// </summary>
		public IcdSecureTcpServer()
		{
			m_TcpClients = new Dictionary<uint, TcpClient>();
			m_ClientBuffers = new Dictionary<uint, byte[]>();
			m_ClientsSection = new SafeCriticalSection();
		}

		#region Methods

		/// <summary>
		/// Starts the TCP Server
		/// </summary>
		public override void Start()
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

				Logger.Log(eSeverity.Notice, "Listening on port {0} with max # of connections {1}",
																Port, MaxNumberOfClients);
			}
			catch (Exception e)
			{
				m_TcpListener = null;

				Logger.Log(eSeverity.Error, "Failed to start listening - {0}", e.Message);
			}
			finally
			{
				UpdateListeningState();
			}
		}

		/// <summary>
		/// Stops the TCP server.
		/// </summary>
		/// <param name="disable">When true disables the TCP server.</param>
		[PublicAPI]
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
				m_TcpListener.Stop();

				IPEndPoint endpoint = m_TcpListener.LocalEndpoint as IPEndPoint;
				if (endpoint == null)
					Logger.Log(eSeverity.Notice, "No longer listening");
				else
					Logger.Log(eSeverity.Notice, "No longer listening on port {0}", endpoint.Port);
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
		public override void Send(string data)
		{
			byte[] byteData = StringUtils.ToBytes(data);

			foreach (uint clientId in GetClients())
			{
				HostInfo hostInfo = GetClientInfo(clientId);
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
		public override void Send(uint clientId, string data)
		{
			byte[] byteData = StringUtils.ToBytes(data);
			HostInfo hostInfo = GetClientInfo(clientId);

			PrintTx(hostInfo, data);
			Send(clientId, byteData);
		}

		/// <summary>
		/// Sets the X509 Certificate to be used by the TCP Listener.
		/// </summary>
		/// <param name="path">The path to read a certificate file from</param>
		public void SetCertificate(string path)
		{
			try
			{
				var certificate = X509Certificate.CreateFromCertFile(path);
				if (!IsValidCertificate(certificate))
					throw new InvalidOperationException("Specified certificate is invalid");

				m_ServerCertificate = certificate;
			}
			catch (Exception e)
			{
				Logger.Log(eSeverity.Error, "Error while setting certificate - {0}:{1}", e.Message,
				           e.InnerException != null ? e.InnerException.Message : "");
			}
		}

		/// <summary>
		/// Generates a new certificate to be used by the TCP Listener.
		/// </summary>
		/// <param name="uuid">The uuid of the device generating the server certificate</param>
		public void GenerateCertificate(string uuid)
		{
			try
			{
				string autoPath = AutoGeneratedCertPath(uuid);
				X509Utils.GenerateAndWriteCertificate(ICD_SECURE_TCP_SERVER_COMMON_NAME, autoPath);
				var autoCertificate = X509Certificate.CreateFromCertFile(autoPath);
				if (!IsValidCertificate(autoCertificate))
					throw new InvalidOperationException("Auto-generated certificate is invalid");

				m_ServerCertificate = autoCertificate;
			}
			catch (Exception e)
			{
				Logger.Log(eSeverity.Error, "Error while generating certificate - {0}:{1}", e.Message,
				           e.InnerException != null ? e.InnerException.Message : "");
			}
		}

		#endregion

		#region Private Methods

		private static bool IsValidCertificate(X509Certificate certificate)
		{
			var expire = DateTime.Parse(certificate.GetExpirationDateString());
			return expire > IcdEnvironment.GetLocalTime();
		}

		/// <summary>
		/// Sends a Byte for Byte string (ISO-8859-1)
		/// </summary>
		/// <param name="clientId">Client Identifier for Connection</param>
		/// <param name="data">String in ISO-8859-1 Format</param>
		/// <returns></returns>
		private void Send(uint clientId, byte[] data)
		{
			if (!ClientConnected(clientId))
			{
				Logger.Log(eSeverity.Error, "Unable to send data to unconnected client {0}", clientId);
				return;
			}

			try
			{
				m_ClientsSection.Execute(() => m_TcpClients[clientId])
								.Client.Send(data, 0, data.Length, SocketFlags.None);
			}
			catch (SocketException ex)
			{
				Logger.Log(eSeverity.Error, ex, "Failed to send data to client {0}", GetClientInfo(clientId));
			}

			if (!ClientConnected(clientId))
				RemoveTcpClient(clientId);
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
				clientId = (uint)IdUtils.GetNewId(m_TcpClients.Keys.Select(i => (int)i), 1);
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

			SslStream clientSslStream = new SslStream(client.GetStream(), false);
			try
			{
				clientSslStream.AuthenticateAsServer(m_ServerCertificate, false, SslProtocols.Default, true);
				clientSslStream.ReadTimeout = 5000;
				clientSslStream.WriteTimeout = 5000;

				clientSslStream.ReadAsync(m_ClientBuffers[clientId], 0, 16384)
							   .ContinueWith(a => TcpClientReceiveHandler(a, clientId));
			}
			catch (AuthenticationException e)
			{
				Logger.Log(eSeverity.Error, "Exception: {0}", e.Message);
				if (e.InnerException != null)
					Logger.Log(eSeverity.Error, "Inner exception: {0}", e.InnerException.Message);
				Logger.Log(eSeverity.Warning, "Authentication failed - closing the connection");
				clientSslStream.Close();
				client.Close();
			}

			// Spawn new thread for accepting new clients
			m_TcpListener.AcceptTcpClientAsync().ContinueWith(TcpClientConnectCallback);

			// let the rest of the application know a new client connected
			IPEndPoint endpoint = (IPEndPoint)client.Client.RemoteEndPoint;
			string host = endpoint.Address.ToString();
			ushort port = (ushort)endpoint.Port;
			HostInfo hostInfo = new HostInfo(host, port);

			AddClient(clientId, hostInfo, SocketStateEventArgs.eSocketStatus.SocketStatusConnected);

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

			DataReceiveEventArgs eventArgs = new DataReceiveEventArgs(clientId, buffer, length);
			HostInfo hostInfo = GetClientInfo(clientId);

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

		#endregion
	}
} 

#endif
