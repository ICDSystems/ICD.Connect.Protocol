#if SIMPLSHARP
using System;
using System.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;
using ICD.Common.Utils;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.Network.Udp
{
	public sealed partial class AsyncUdpClient
	{
		private UDPServer m_UdpClient;

		#region Methods

		/// <summary>
		/// Connects to the end point.
		/// </summary>
		public override void Connect()
		{
			Disconnect();

			string address = Address;
			ushort port = Port;

			try
			{
				m_UdpClient = new UDPServer(address, port, BufferSize);
				m_UdpClient.EnableUDPServer();

				// Spawn new listening thread
				m_ListeningRequested = true;
				SocketErrorCodes error = m_UdpClient.ReceiveDataAsync(UdpClientReceiveHandler);

				if (error != SocketErrorCodes.SOCKET_OK &&
				    error != SocketErrorCodes.SOCKET_CONNECTION_IN_PROGRESS &&
				    error != SocketErrorCodes.SOCKET_OPERATION_PENDING)
				{
					Log(eSeverity.Error, "Failed to connect to {0}:{1} - {2}",
					    address, port, error);
					Disconnect();
				}
			}
			catch (Exception e)
			{
				Log(eSeverity.Error, "Failed to connect to {0}:{1} - {2}",
				    address, port, e.Message);
				Disconnect();
			}

			UpdateIsConnectedState();
		}

		/// <summary>
		/// Disconnects from the end point.
		/// </summary>
		public override void Disconnect()
		{
			m_ListeningRequested = false;

			if (m_UdpClient != null)
			{
				m_UdpClient.DisableUDPServer();
				m_UdpClient.Dispose();
			}
			m_UdpClient = null;

			UpdateIsConnectedState();
		}

		/// <summary>
		/// Returns the connection state of the port
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsConnectedState()
		{
			if (m_UdpClient == null)
				return false;

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
		}

		/// <summary>
		/// Implements the actual sending logic. Wrapped by SendToAddress to handle connection status.
		/// </summary>
		private bool SendToAddressFinal(string data, string ipAddress, int port)
		{
			byte[] bytes = StringUtils.ToBytes(data);

			try
			{
				m_UdpClient.SendData(bytes, bytes.Length, ipAddress, port);
				PrintTx(new HostInfo(ipAddress, (ushort)port).ToString(), data);
				return true;
			}
			catch (Exception e)
			{
				Log(eSeverity.Error, "Failed to send data to {0}:{1} - {2}",
				    ipAddress, port, e.Message);
			}

			return false;
		}

		/// <summary>
		/// Implements the actual sending logic. Wrapped by Send to handle connection status.
		/// </summary>
		protected override bool SendFinal(string data)
		{
			byte[] bytes = StringUtils.ToBytes(data);

			try
			{
				m_UdpClient.SendData(bytes, bytes.Length);
				PrintTx(data);
				return true;
			}
			catch (Exception e)
			{
				Log(eSeverity.Error, "Failed to send data to {0}:{1} - {2}",
				    m_UdpClient.AddressToAcceptConnectionFrom, m_UdpClient.PortNumber, e.Message);
			}

			return false;
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Handles Receiving Data from the Active TCP Connection
		/// </summary>
		/// <param name="udpClient"></param>
		/// <param name="bytesReceived"></param>
		private void UdpClientReceiveHandler(UDPServer udpClient, int bytesReceived)
		{
			if (bytesReceived <= 0)
				return;

			string data = StringUtils.ToString(udpClient.IncomingDataBuffer.Take(bytesReceived));

			HostInfo host = new HostInfo(udpClient.IPAddressLastMessageReceivedFrom,
			                             (ushort)udpClient.IPPortLastMessageReceivedFrom);

			PrintRx(host.ToString(), data);
			Receive(data);

			SocketErrorCodes socketError = udpClient.ReceiveDataAsync(UdpClientReceiveHandler);
			if (socketError != SocketErrorCodes.SOCKET_OPERATION_PENDING)
			{
				ErrorLog.Error("AsyncUdpClient({0}:{1}) failed to ReceiveDataAsync", udpClient.LocalAddressOfServer,
				               udpClient.PortNumber);
			}

			UpdateIsConnectedState();
		}

		#endregion
	}
}

#endif
