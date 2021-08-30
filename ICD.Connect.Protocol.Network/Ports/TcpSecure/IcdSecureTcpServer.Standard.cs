#if !SIMPLSHARP
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
using ICD.Common.Utils.IO;
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
		private const int SSL_STREAM_READ_TIMEOUT = 5000;
		private const int SSL_STREAM_WRITE_TIMEOUT = 5000;

		private X509Certificate m_ServerCertificate;

		private TcpListenerEx m_TcpListener;

		private readonly Dictionary<uint, Tuple<TcpClient, SslStream, byte[]>> m_Clients;
		private readonly SafeCriticalSection m_ClientsSection;

		/// <summary>
		/// Constructor.
		/// </summary>
		public IcdSecureTcpServer()
		{
			m_Clients = new Dictionary<uint, Tuple<TcpClient, SslStream, byte[]>>();
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
				GenerateCertificate();

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
		/// Called in a worker thread to send the data to the specified client
		/// This should send the data synchronously to ensure in-order transmission
		/// If this blocks, it will stop all data from being sent
		/// </summary>
		/// <param name="clientId"></param>
		/// <param name="data"></param>
		protected override void SendWorkerAction(uint clientId, string data)
		{
			byte[] byteData = StringUtils.ToBytes(data);
			HostInfo hostInfo;
			TryGetClientInfo(clientId, out hostInfo);

			PrintTx(hostInfo, data);
			SendWorkerAction(clientId, byteData);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Generates a new certificate to be used by the TCP Listener.
		/// </summary>
		private void GenerateCertificate()
		{
			try
			{
				string autoPath = AutoGeneratedPfxPath();

				// Load the existing cert
				if (IcdFile.Exists(autoPath))
				{
					X509Certificate existing = X509Certificate.CreateFromCertFile(autoPath);
					if (IsValidCertificate(existing))
					{
						m_ServerCertificate = existing;
						return;
					}
				}

				// Create the directory
				string directory = IcdPath.GetDirectoryName(autoPath);
				if (!string.IsNullOrEmpty(directory))
					IcdDirectory.CreateDirectory(directory);

				// Make a new cert
				X509Utils.GenerateAndWriteCertificate(ICD_SECURE_TCP_SERVER_COMMON_NAME, autoPath);
				X509Certificate autoCertificate = X509Certificate.CreateFromCertFile(autoPath);
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

		/// <summary>
		/// Returns true if the given certificates passes custom validation.
		/// </summary>
		/// <param name="certificate"></param>
		/// <returns></returns>
		private static bool IsValidCertificate(X509Certificate certificate)
		{
			DateTime expire = DateTime.Parse(certificate.GetExpirationDateString());
			return expire > IcdEnvironment.GetUtcTime();
		}

		/// <summary>
		/// Sends a Byte for Byte string (ISO-8859-1)
		/// </summary>
		/// <param name="clientId">Client Identifier for Connection</param>
		/// <param name="data">String in ISO-8859-1 Format</param>
		/// <returns></returns>
		private void SendWorkerAction(uint clientId, byte[] data)
		{
			HostInfo info;
			if (!TryGetClientInfo(clientId, out info))
			{
				Logger.Log(eSeverity.Error, "Unable to send data to unconnected client {0}", clientId);
				return;
			}

			try
			{
				m_ClientsSection.Execute(() => m_Clients[clientId])
				                .Item2.Write(data);
			}
			catch (IOException ex)
			{
				Logger.Log(eSeverity.Error, ex, "Failed to write data to ssl stream for client {0}", info);
			}
			catch (InvalidOperationException ex)
			{
				Logger.Log(eSeverity.Error, ex, "Failed to send data to client {0}: Authentication has not occured yet", info);
			}
			catch (NotSupportedException ex)
			{
				Logger.Log(eSeverity.Error, ex, "Failed to write data to ssl stream for client {0}: Write operation already in progress", info);
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
			if (task.Status == TaskStatus.Faulted)
				return;

			uint clientId;

			TcpClient client = task.Result;
			SslStream clientSslStream = new SslStream(client.GetStream(), false);
			byte[] clientBuffer = new byte[BufferSize];

			m_ClientsSection.Enter();

			try
			{
				clientId = (uint)IdUtils.GetNewId(m_Clients.Keys.Select(i => (int)i), 1);
				m_Clients[clientId] = new Tuple<TcpClient, SslStream, byte[]>(client, clientSslStream, clientBuffer);
			}
			finally
			{
				m_ClientsSection.Leave();
			}

			try
			{
				clientSslStream.AuthenticateAsServer(m_ServerCertificate, false, SslProtocols.Default, true);
				clientSslStream.ReadTimeout = SSL_STREAM_READ_TIMEOUT;
				clientSslStream.WriteTimeout = SSL_STREAM_WRITE_TIMEOUT;

				clientSslStream.ReadAsync(clientBuffer, 0, BufferSize)
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

			Tuple<TcpClient, SslStream, byte[]> clientData = null;

			if (!m_ClientsSection.Execute(() => m_Clients.TryGetValue(clientId, out clientData)))
			{
				// Specified Client Doesn't exits?
				RemoveTcpClient(clientId);
				return;
			}

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

			DataReceiveEventArgs eventArgs = new DataReceiveEventArgs(clientId, clientData.Item3, length);
			HostInfo hostInfo;
			TryGetClientInfo(clientId, out hostInfo);

			PrintRx(hostInfo, eventArgs.Data);
			RaiseOnDataReceived(eventArgs);

			if (!ClientConnected(clientId))
			{
				RemoveTcpClient(clientId);
				return;
			}

			// Spawn a new listening thread
			clientData.Item2.ReadAsync(clientData.Item3, 0, BufferSize)
							.ContinueWith(a => TcpClientReceiveHandler(a, clientId));

			UpdateListeningState();
		}

		private void RemoveTcpClient(uint clientId)
		{
			m_ClientsSection.Enter();
			try
			{
				Tuple<TcpClient, SslStream, byte[]> client;
				if (m_Clients.TryGetValue(clientId, out client))
				{
					m_Clients.Remove(clientId);
					client.Item2.Dispose();
					client.Item1.Dispose();
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
