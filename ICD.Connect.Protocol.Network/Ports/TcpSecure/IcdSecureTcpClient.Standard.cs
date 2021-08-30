#if !SIMPLSHARP
using System;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using ICD.Common.Utils;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Protocol.Network.Ports.TcpSecure
{
	public sealed partial class IcdSecureTcpClient
	{
		private TcpClient m_TcpClient;
		private SslStream m_Stream;
		private readonly byte[] m_Buffer = new byte[DEFAULT_BUFFER_SIZE];
		private CancellationTokenSource m_Cancellation;

		private static bool ValidateServerCertificate(
			object sender,
			X509Certificate certificate,
			X509Chain chain,
			SslPolicyErrors sslPolicyErrors)
		{
			return true;
		}

		/// <summary>
		/// Connects to the remote end point.
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
				m_TcpClient = new TcpClient(Address, Port);
				m_Cancellation = new CancellationTokenSource();

				m_Stream = new SslStream(m_TcpClient.GetStream(), false,
				                         ValidateServerCertificate, null);
				try
				{
					m_Stream.AuthenticateAsClient(Address);
				}
				catch (AuthenticationException e)
				{
					Logger.Log(eSeverity.Error, "Exception: {0}", e.Message);
					if (e.InnerException != null)
						Logger.Log(eSeverity.Error, "Inner exception: {0}", e.InnerException.Message);
					Logger.Log(eSeverity.Warning, "Authentication failed - closing the connection");
					m_TcpClient.Close();
				}

				m_Stream.ReadAsync(m_Buffer, 0, m_Buffer.Length, m_Cancellation.Token)
				        .ContinueWith(TcpClientReceiveHandler, m_Cancellation.Token);
			}
			catch (AggregateException ae)
			{
				ae.Handle(x =>
				{
					if (x is SocketException)
					{
						Logger.Log(eSeverity.Error, "Failed to connect to host {0}:{1} - {2}", Address, Port, x.Message);
						return true;
					}

					return false;
				});
			}
			catch (Exception e)
			{
				Logger.Log(eSeverity.Error, "Failed to connect to host {0}:{1} - {2}", Address, Port, e.Message);
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
			m_Cancellation?.Cancel();

			m_Stream?.Dispose();
			m_Stream = null;

			m_TcpClient?.Dispose();
			m_TcpClient = null;

			UpdateIsConnectedState();
		}

		/// <summary>
		/// Sends a Byte for Byte string (ISO-8859-1)
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		private void  SendWorkerAction(string data)
		{
			byte[] bytes = StringUtils.ToBytes(data);
			try
			{
				if (m_Stream == null)
				{
					Logger.Log(eSeverity.Error, "Failed to send data - no SSLStream");
					return;
				}

				PrintTx(data);
				m_Stream.Write(bytes);
			}
			catch (SocketException e)
			{
				Logger.Log(eSeverity.Error, "Failed to send data - {0}", e.Message);
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
				Logger.Log(eSeverity.Error, "Failed to receive data from host {0}:{1} - {2}", Address, Port, message);
				UpdateIsConnectedState();
				return;
			}

			int bytesRead = task.Result;
			if (bytesRead <= 0)
				return;

			string data = StringUtils.ToString(m_Buffer, bytesRead);

			PrintRx(data);
			Receive(data);

			if (m_TcpClient?.Connected ?? false)
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

			addRow("Buffer Size", BufferSize);
		}
	}
} 

#endif
