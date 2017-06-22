﻿using System;
using System.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;
using ICD.Common.Properties;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;

#if SIMPLSHARP

namespace ICD.Connect.Protocol.Network.Udp
{
	public sealed partial class AsyncUdpClient
	{
		private UDPServer m_UdpClient;

		#region Properties

		/// <summary>
		/// Address to accept connections from.
		/// </summary>
		[PublicAPI]
		public string Address
		{
			get { return m_Address; }
			set { m_Address = IcdEnvironment.NetworkAddresses.Contains(m_Address) ? "127.0.0.1" : value; }
		}

		#endregion

		#region Methods

		/// <summary>
		/// Connects to the end point.
		/// </summary>
		public override void Connect()
		{
			Disconnect();

			try
			{
				m_UdpClient = new UDPServer(Address, Port, BufferSize);
				m_UdpClient.EnableUDPServer();

				// Spawn new listening thread
				m_ListeningRequested = true;
				m_UdpClient.ReceiveDataAsync(UdpClientReceiveHandler);
			}
			catch (Exception e)
			{
				Logger.AddEntry(eSeverity.Error, "{0} failed to connect - {1}", this, e.Message);
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
					return false;

					// I'm not 100% on these
				case SocketStatus.SOCKET_STATUS_WAITING:
				case SocketStatus.SOCKET_STATUS_CONNECTED:
				case SocketStatus.SOCKET_STATUS_DNS_LOOKUP:
				case SocketStatus.SOCKET_STATUS_DNS_RESOLVED:
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

			m_UdpClient.SendData(bytes, bytes.Length, ipAddress, port);
			PrintTx(data);

			return true;
		}

		/// <summary>
		/// Implements the actual sending logic. Wrapped by Send to handle connection status.
		/// </summary>
		protected override bool SendFinal(string data)
		{
			byte[] bytes = StringUtils.ToBytes(data);

			PrintTx(data);
			m_UdpClient.SendData(bytes, bytes.Length);

			return true;
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

			PrintRx(data);
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
