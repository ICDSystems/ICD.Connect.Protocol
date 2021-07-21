using System;
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
	public sealed class IcdUdpSocket : IDisposable
	{
		public const ushort DEFAULT_BUFFER_SIZE = 16384;

		public event EventHandler<BoolEventArgs> OnIsConnectedStateChanged;
		public event EventHandler<UdpDataReceivedEventArgs> OnDataReceived; 

		private readonly ushort m_Port;

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

				OnIsConnectedStateChanged.Raise(this, m_IsConnected);
			}
		}

		public ushort Port { get { return m_Port; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="port"></param>
		public IcdUdpSocket(ushort port)
		{
			m_Port = port;
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			OnIsConnectedStateChanged = null;
			OnDataReceived = null;

			Disconnect();
		}

		#region Methods

		/// <summary>
		/// Connects to the end point.
		/// </summary>
		public void Connect()
		{
			Disconnect();

			try
			{
#if SIMPLSHARP
				m_UdpClient = new UDPServer("0.0.0.0", m_Port, DEFAULT_BUFFER_SIZE);
				m_UdpClient.EnableUDPServer();

				// Spawn new listening thread
				SocketErrorCodes error = m_UdpClient.ReceiveDataAsync(UdpClientReceiveHandler);

				if (error != SocketErrorCodes.SOCKET_OK &&
				    error != SocketErrorCodes.SOCKET_CONNECTION_IN_PROGRESS &&
				    error != SocketErrorCodes.SOCKET_OPERATION_PENDING)
				{
					throw new Exception(error.ToString());
				}
#else
				m_UdpClient = new UdpClient(m_Port) { EnableBroadcast = true };
				m_UdpClient.ReceiveAsync().ContinueWith(UdpClientReceiveHandler);
#endif
			}
			catch (Exception)
			{
				Disconnect();
				throw;
			}

			UpdateIsConnectedState();
		}

		/// <summary>
		/// Disconnects from the end point.
		/// </summary>
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
		public void Send(string data)
		{
			if (m_UdpClient == null)
				throw new InvalidOperationException("Wrapped client is null");

			byte[] bytes = StringUtils.ToBytes(data);
#if SIMPLSHARP
			m_UdpClient.SendData(bytes, bytes.Length);
#else
			m_UdpClient.Send(bytes, bytes.Length);
#endif
		}

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