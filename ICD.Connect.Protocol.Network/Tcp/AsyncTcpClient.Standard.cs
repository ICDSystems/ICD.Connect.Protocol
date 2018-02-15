
#if STANDARD
using System;
using System.Linq;
using System.Net.Sockets;
using ICD.Common.Utils;
using System.Threading.Tasks;
using ICD.Connect.API.Nodes;
using ICD.Common.Utils.Services.Logging;

namespace ICD.Connect.Protocol.Network.Tcp
{
	public sealed partial class AsyncTcpClient
	{
		private TcpClient m_TcpClient;
		private NetworkStream m_Stream;
		private readonly byte[] m_Buffer = new byte[DEFAULT_BUFFER_SIZE];

		/// <summary>
		/// Connects to the remote end point Asyncrohnously
		/// </summary>
		/// <returns></returns>
		public override void Connect()
		{
			Disconnect();

			if (!m_SocketMutex.WaitForMutex(1000))
			{
				Logger.AddEntry(eSeverity.Error, "{0} failed to obtain SocketMutex for connect", this);
				return;
			}

			try
			{
				m_TcpClient = new TcpClient();
				m_TcpClient.ConnectAsync(Address, Port).Wait();

				if (!m_TcpClient.Connected)
				{
					Logger.AddEntry(eSeverity.Error, "{0} failed to connect to {1}:{2}", this, Address, Port);
					return;
				}

				m_Stream = m_TcpClient.GetStream();
				m_Stream.ReadAsync(m_Buffer, 0, m_Buffer.Length).ContinueWith(TcpClientReceiveHandler);
			}
			catch (Exception e)
			{
				Logger.AddEntry(eSeverity.Error, "{0} failed to connect to host {1}:{2} - {3}", this, Address, Port, e.Message);
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
			return m_TcpClient != null && m_TcpClient.Client.Connected;
		}

		/// <summary>
		/// Disconnects and clears the existing TCP Client instance.
		/// </summary>
		private void DisposeTcpClient()
		{
			if (m_Stream != null)
				m_Stream.Dispose();
			m_Stream = null;

			if (m_TcpClient != null)
				m_TcpClient.Dispose();
			m_TcpClient = null;

			UpdateIsConnectedState();
		}

		/// <summary>
		/// Sends a Byte for Byte string (ISO-8859-1)
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		protected override bool SendFinal(string data)
		{
			byte[] bytes = StringUtils.ToBytes(data);
			try
			{
				m_TcpClient.Client.Send(bytes, 0, bytes.Length, SocketFlags.None);
				PrintTx(data);
				return true;
			}
			finally
			{
				UpdateIsConnectedState();
			}
		}

		/// <summary>
		/// Handles Receiving Data from the Active TCP Connection
		/// </summary>
		/// <param name="task"></param>
		private void TcpClientReceiveHandler(Task<int> task)
		{
			if (task.IsFaulted)
			{
				string message = task.Exception.InnerExceptions.First().Message;
				Logger.AddEntry(eSeverity.Error, "{0} failed to receive data from host {1}:{2} - {3}", this, Address, Port,
								message);
				UpdateIsConnectedState();
				return;
			}

			int bytesRead = task.Result;
			if (bytesRead <= 0)
				return;

			string data = StringUtils.ToString(m_Buffer, bytesRead);

			PrintRx(data);
			Receive(data);

			if (m_TcpClient.Connected)
				m_Stream.ReadAsync(m_Buffer, 0, m_Buffer.Length).ContinueWith(TcpClientReceiveHandler);

			UpdateIsConnectedState();
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Address", Address);
			addRow("Port", Port);
			addRow("Buffer Size", BufferSize);
		}
	}
}
#endif
