﻿
#if STANDARD
using System;
using System.Net.Sockets;
using ICD.SimplSharp.Common.Utils;
using System.Threading.Tasks;
using ICD.SimplSharp.Common.Console.Nodes;
using ICD.SimplSharp.Common.Services.Logging;

namespace ICD.SimplSharp.API.Ports.Tcp
{
	public sealed partial class AsyncTcpClient
	{
		private TcpClient m_TcpClient;
        private NetworkStream m_Stream;
        private byte[] m_Buffer = new byte[DEFAULT_BUFFER_SIZE];

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
				Logger.AddEntry(eSeverity.Error, e, "{0} failed to connect to host {1}:{2}", this,
									  m_TcpClient,
									  m_TcpClient);
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

			return m_TcpClient.Client.Connected;
        }

        /// <summary>
        /// Disconnects and clears the existing TCP Client instance.
        /// </summary>
        private void DisposeTcpClient()
        {
            if (m_TcpClient == null)
                return;

            m_Stream.Dispose();
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
		/// <param name="tcpClient"></param>
		/// <param name="task"></param>
		private void TcpClientReceiveHandler(Task<int> task)
        {
            if (task.IsFaulted)
            {
                Logger.AddEntry(eSeverity.Error, "{0} failed to receive data from host {1}:{2}", this,
                                  Address, Port);
            }
            int bytesRead = task.Result;
            if (bytesRead <= 0)
                return;

            string data = StringUtils.ToString(m_Buffer, bytesRead);

            PrintRx(data);
            Receive(data);

            if(m_TcpClient.Connected)
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
