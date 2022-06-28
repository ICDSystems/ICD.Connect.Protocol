using System;
using ICD.Common.Properties;
using ICD.Connect.Protocol.Heartbeat;
#if SIMPLSHARP
using System.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;
using UdpClient = Crestron.SimplSharp.CrestronSockets.UDPServer;
#else
using System.Net.Sockets;
using System.Threading.Tasks;
using UdpClient = System.Net.Sockets.UdpClient;
#endif
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.Network.EventArguments;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.Network.Ports.Udp
{
	public sealed class IcdUdpSocket : IConnectable, IDisposable
	{
		public const ushort DEFAULT_BUFFER_SIZE = 16384;
		public const string DEFAULT_ACCEPT_ADDRESS = "0.0.0.0";
		public const ushort EPHEMERAL_LOCAL_PORT = 0;

		public event EventHandler<BoolEventArgs> OnConnectedStateChanged;
		public event EventHandler<UdpDataReceivedEventArgs> OnDataReceived;

		private readonly string m_AcceptAddress;
		private readonly ushort m_RemotePort;
		private readonly ushort m_LocalPort;

		private UdpClient m_UdpClient;
		private bool m_IsConnected;

		#region Properties

		public bool IsConnected
		{
			get { return m_IsConnected; }
			private set
			{
				if (value == m_IsConnected)
					return;

				m_IsConnected = value;

				OnConnectedStateChanged.Raise(this, m_IsConnected);
			}
		}

		public ushort RemotePort { get { return m_RemotePort; } }

		public ushort LocalPort { get { return m_LocalPort; } }

		public string AcceptAddress { get { return m_AcceptAddress; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="localPort"></param>
		/// <param name="remotePort"></param>
		/// <param name="acceptAddress"></param>
		[PublicAPI]
		public IcdUdpSocket(string acceptAddress, ushort localPort, ushort remotePort)
		{
			m_AcceptAddress = acceptAddress;
			m_LocalPort = localPort;
			m_RemotePort = remotePort;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="remotePort"></param>
		[PublicAPI]
		public IcdUdpSocket(ushort remotePort): this(DEFAULT_ACCEPT_ADDRESS, EPHEMERAL_LOCAL_PORT, remotePort)
		{
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="acceptAddress"></param>
		/// <param name="remotePort"></param>
		[PublicAPI]
		public IcdUdpSocket(string acceptAddress, ushort remotePort) : this(acceptAddress, EPHEMERAL_LOCAL_PORT, remotePort)
		{
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			OnConnectedStateChanged = null;
			OnDataReceived = null;

			Disconnect();
		}

		#region Methods

		/// <summary>
		/// Connects to the end point.
		/// </summary>
		[PublicAPI]
		public bool Connect()
		{
			Disconnect();

			try
			{
#if SIMPLSHARP
				m_UdpClient = new UDPServer(AcceptAddress, LocalPort, DEFAULT_BUFFER_SIZE, RemotePort);
				m_UdpClient.EnableUDPServer();

				// Spawn new listening thread
				SocketErrorCodes error = m_UdpClient.ReceiveDataAsync(UdpClientReceiveHandler);

				if (error != SocketErrorCodes.SOCKET_OK &&
				    error != SocketErrorCodes.SOCKET_CONNECTION_IN_PROGRESS &&
				    error != SocketErrorCodes.SOCKET_OPERATION_PENDING)
				{
					//Todo: Log Error here
				}
#else
				m_UdpClient = new UdpClient(LocalPort) { EnableBroadcast = true };
				m_UdpClient.ReceiveAsync().ContinueWith(UdpClientReceiveHandler);
#endif
			}
			catch (Exception)
			{
				Disconnect();
				//Todo: Log Exception here
				return false;
			}

			UpdateIsConnectedState();
			return IsConnected;
		}

		/// <summary>
		/// Connect the instance to the remote endpoint.
		/// </summary>
		void IConnectable.Connect()
		{
			Connect();
		}

		/// <summary>
		/// Disconnects from the end point.
		/// </summary>
		[PublicAPI]
		public void Disconnect()
		{
			if (m_UdpClient != null)
			{
#if SIMPLSHARP
				m_UdpClient.DisableUDPServer();
#endif
				m_UdpClient.Dispose();
			}
			m_UdpClient = null;

			UpdateIsConnectedState();
		}

		/// <summary>
		/// Implements the actual sending logic. Wrapped by Send to handle connection status.
		/// </summary>
		[PublicAPI]
		public void Send(string data)
		{
			if (m_UdpClient == null)
				throw new InvalidOperationException("Wrapped client is null");

			byte[] bytes = StringUtils.ToBytes(data);
#if SIMPLSHARP
			m_UdpClient.SendData(bytes, bytes.Length, AcceptAddress, RemotePort);
#else
			m_UdpClient.Send(bytes, bytes.Length, AcceptAddress, RemotePort);
#endif
		}

		/// <summary>
		/// Implements the actual sending logic for sending to specific address/port
		/// 
		/// </summary>
		/// <param name="data"></param>
		/// <param name="ipAddress"></param>
		/// <param name="port"></param>
		[PublicAPI]
		public void SendToAddress(string data, string ipAddress, int port)
		{
			if (m_UdpClient == null)
				throw new InvalidOperationException("Wrapped client is null");

			byte[] bytes = StringUtils.ToBytes(data);
#if SIMPLSHARP
			m_UdpClient.SendData(bytes, bytes.Length, ipAddress, port);
#else
			m_UdpClient.Send(bytes, bytes.Length, ipAddress, port);
#endif
		}

		#endregion

#region Private Methods

#if SIMPLSHARP
		/// <summary>
		/// Handles Receiving Data from the Active UDP Connection
		/// </summary>
		/// <param name="udpClient"></param>
		/// <param name="bytesReceived"></param>
		private void UdpClientReceiveHandler(UdpClient udpClient, int bytesReceived)
		{
			if (bytesReceived <= 0)
				return;

			string data = StringUtils.ToString(udpClient.IncomingDataBuffer.Take(bytesReceived));

			HostInfo host = new HostInfo(udpClient.IPAddressLastMessageReceivedFrom,
										 (ushort)udpClient.IPPortLastMessageReceivedFrom);

			OnDataReceived.Raise(this, new UdpDataReceivedEventArgs(host, data));

			SocketErrorCodes socketError = udpClient.ReceiveDataAsync(UdpClientReceiveHandler);
			if (socketError != SocketErrorCodes.SOCKET_OPERATION_PENDING)
			{
				ErrorLog.Error("AsyncUdpClient({0}:{1}) failed to ReceiveDataAsync", udpClient.LocalAddressOfServer,
							   udpClient.PortNumber);
			}

			UpdateIsConnectedState();
		}
#else
		/// <summary>
		/// Handles Receiving Data from the Active TCP Connection
		/// </summary>
		/// <param name="task"></param>
		private void UdpClientReceiveHandler(Task<UdpReceiveResult> task)
		{
			UdpReceiveResult result;

			try
			{
				result = task.Result;
			}
			catch (AggregateException ae)
			{
				ae.Handle(e =>
				{
					// Connection forcibly closed
					if (e is SocketException)
						return true;

					// We stopped the connection
					if (e is ObjectDisposedException)
						return true;

					return false;
				});
			}
			finally
			{
				UpdateIsConnectedState();
			}

			if (result.Buffer == null || result.Buffer.Length <= 0)
				return;

			string data = StringUtils.ToString(result.Buffer);
			HostInfo hostInfo = new HostInfo(result.RemoteEndPoint.Address.ToString(),
			                                 (ushort)result.RemoteEndPoint.Port);

			OnDataReceived.Raise(this, new UdpDataReceivedEventArgs(hostInfo, data));

			try
			{
				m_UdpClient.ReceiveAsync().ContinueWith(UdpClientReceiveHandler);
			}
			catch (SocketException)
			{
			}

			UpdateIsConnectedState();
		}
#endif

		private void UpdateIsConnectedState()
		{
			IsConnected = GetIsConnectedState();
		}

		/// <summary>
		/// Returns the connection state of the port
		/// </summary>
		/// <returns></returns>
		private bool GetIsConnectedState()
		{
			if (m_UdpClient == null)
				return false;

#if SIMPLSHARP
			switch (m_UdpClient.ServerStatus)
			{
				case SocketStatus.SOCKET_STATUS_NO_CONNECT:
				case SocketStatus.SOCKET_STATUS_DNS_FAILED:
				case SocketStatus.SOCKET_STATUS_LINK_LOST:
				case SocketStatus.SOCKET_STATUS_CONNECT_FAILED:
				case SocketStatus.SOCKET_STATUS_BROKEN_REMOTELY:
				case SocketStatus.SOCKET_STATUS_BROKEN_LOCALLY:
				case SocketStatus.SOCKET_STATUS_SOCKET_NOT_EXIST:
				case SocketStatus.SOCKET_STATUS_WAITING:
				case SocketStatus.SOCKET_STATUS_DNS_LOOKUP:
				case SocketStatus.SOCKET_STATUS_DNS_RESOLVED:
					return false;
				case SocketStatus.SOCKET_STATUS_CONNECTED:
					return true;

				default:
					throw new ArgumentOutOfRangeException();
			}
#else
			return true;
#endif
		}

		#endregion
	}
}