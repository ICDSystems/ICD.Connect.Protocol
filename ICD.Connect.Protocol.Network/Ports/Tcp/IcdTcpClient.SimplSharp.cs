using ICD.Common.Logging.LoggingContexts;
#if SIMPLSHARP
using System;
using Crestron.SimplSharp.CrestronSockets;
using ICD.Common.Utils;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Protocol.Network.Ports.Tcp
{
	public sealed partial class IcdTcpClient
	{
		private TCPClient m_TcpClient;

		/// <summary>
		/// Connects to the remote end point synchronously.
		/// </summary>
		/// <returns></returns>
		public override void Connect()
		{
			Disconnect();

			if (!m_SocketMutex.WaitForMutex(1000))
			{
				Logger.Log(eSeverity.Error, "Failed to obtain SocketMutex for connect");
				return;
			}

			try
			{
				m_TcpClient = new TCPClient(Address, Port, BufferSize);
				Subscribe(m_TcpClient);

				SocketErrorCodes result = m_TcpClient.ConnectToServer();

				if (m_TcpClient.ClientStatus != SocketStatus.SOCKET_STATUS_CONNECTED)
				{
					Logger.Log(eSeverity.Error, "Failed to connect with error code {0}", result);
					return;
				}

				m_TcpClient.ReceiveDataAsync(TcpClientReceiveHandler);
			}
			catch (Exception e)
			{
				Logger.Log(eSeverity.Error, "Failed to connect to host {0}:{1} - {2}",
				           m_TcpClient.AddressClientConnectedTo,
				           m_TcpClient.PortNumber,
				           e.Message);
			}
			finally
			{
				m_SocketMutex.ReleaseMutex();

				UpdateIsConnectedState();
			}
		}

		/// <summary>
		/// Gets the current connection state of the wrapped TCP client.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsConnectedState()
		{
			if (m_TcpClient == null)
				return false;

			switch (m_TcpClient.ClientStatus)
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
		/// Disconnects and clears the existing TCP Client instance.
		/// </summary>
		private void DisposeTcpClient()
		{
			if (m_TcpClient == null)
				return;

			Unsubscribe(m_TcpClient);

			m_TcpClient.DisconnectFromServer();
			m_TcpClient.Dispose();

			m_TcpClient = null;

			UpdateIsConnectedState();
		}

		/// <summary>
		/// Sends a Byte for Byte string (ISO-8859-1)
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		private void SendWorkerAction(string data)
		{
			byte[] bytes = StringUtils.ToBytes(data);
			try
			{
				PrintTx(data);
				m_TcpClient.SendData(bytes, bytes.Length);
			}
			finally
			{
				UpdateIsConnectedState();
			}
		}

#region TCPClient Callbacks

		/// <summary>
		/// Subscribe to the TCPClient events.
		/// </summary>
		/// <param name="tcpClient"></param>
		private void Subscribe(TCPClient tcpClient)
		{
			tcpClient.SocketStatusChange += TcpClientOnSocketStateChange;
		}

		/// <summary>
		/// Unsubscribe from the TCPClient events.
		/// </summary>
		/// <param name="tcpClient"></param>
		private void Unsubscribe(TCPClient tcpClient)
		{
			tcpClient.SocketStatusChange -= TcpClientOnSocketStateChange;
		}

		/// <summary>
		/// Determines if the local socket state differs from that of the TCP Client and then takes action if necessary
		/// </summary>
		/// <param name="tcpClient"></param>
		/// <param name="newSocketState"></param>
		private void TcpClientOnSocketStateChange(TCPClient tcpClient, SocketStatus newSocketState)
		{
			UpdateIsConnectedState();
		}

		/// <summary>
		/// Handles Receiving Data from the Active TCP Connection
		/// </summary>
		/// <param name="tcpClient"></param>
		/// <param name="bytesReceived"></param>
		private void TcpClientReceiveHandler(TCPClient tcpClient, int bytesReceived)
		{
			try
			{
				if (bytesReceived <= 0)
					return;

				string data = StringUtils.ToString(tcpClient.IncomingDataBuffer, bytesReceived);

				PrintRx(data);
				Receive(data);
			}
			catch (Exception e)
			{
				Logger.Log(eSeverity.Error, e, "Exception occurred while processing received data");
			}

			SocketErrorCodes socketError = tcpClient.ReceiveDataAsync(TcpClientReceiveHandler);

			switch (socketError)
			{
				case SocketErrorCodes.SOCKET_OK:
				case SocketErrorCodes.SOCKET_OPERATION_PENDING:
					break;

				default:
					Logger.Log(eSeverity.Error, "Failed to ReceiveDataAsync from host {0}:{1} - {2}",
					           tcpClient.AddressClientConnectedTo,
					           tcpClient.PortNumber,
					           socketError);
					break;
			}

			UpdateIsConnectedState();
		}

#endregion

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Buffer Size", BufferSize);
			addRow("Client Status", m_TcpClient == null ? string.Empty : m_TcpClient.ClientStatus.ToString());
			addRow("Local Port", m_TcpClient == null ? 0 : m_TcpClient.LocalPortNumberOfClient);
		}
	}
}

#endif
