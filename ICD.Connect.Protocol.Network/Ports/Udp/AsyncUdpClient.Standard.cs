#if !SIMPLSHARP
using System;
using ICD.Common.Utils;
using System.Net.Sockets;
using System.Threading.Tasks;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.Network.Ports.Udp
{
	public sealed partial class AsyncUdpClient
	{
		private UdpClient m_UdpClient;
		private readonly SafeCriticalSection m_ClientSection = new SafeCriticalSection();

		/// <summary>
		/// Connects to the end point.
		/// </summary>
		public override void Connect()
		{
			m_ClientSection.Enter();

			try
			{
				Disconnect();

				m_UdpClient = new UdpClient(Port) {EnableBroadcast = true};

				// Spawn new listening thread
				m_ListeningRequested = true;
				m_UdpClient.ReceiveAsync().ContinueWith(UdpClientReceiveHandler);
			}
			catch (Exception e)
			{
				Log(eSeverity.Error, "Failed to connect - {0}", e.Message);
			}
			finally
			{
				m_ClientSection.Leave();
			}

			UpdateIsConnectedState();
		}

		/// <summary>
		/// Disconnects from the end point.
		/// </summary>
		public override void Disconnect()
		{
			m_ClientSection.Enter();

			try
			{
				m_ListeningRequested = false;

				if (m_UdpClient != null)
					m_UdpClient.Dispose();

				m_UdpClient = null;
			}
			finally
			{
				m_ClientSection.Leave();
			}

			UpdateIsConnectedState();
		}

		/// <summary>
		/// Returns the connection state of the port
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsConnectedState()
		{
			return m_ClientSection.Execute(() => m_UdpClient != null);
		}

		/// <summary>
		/// Implements the actual sending logic. Wrapped by SendToAddress to handle connection status.
		/// </summary>
		private bool SendToAddressFinal(string data, string ipAddress, int port)
		{
			byte[] bytes = StringUtils.ToBytes(data);

			m_ClientSection.Enter();

			try
			{
				m_UdpClient.Send(bytes, bytes.Length, ipAddress, port);
				PrintTx(new HostInfo(ipAddress, (ushort)port).ToString(), data);
			}
			finally
			{
				m_ClientSection.Leave();
			}

			return true;
		}

		/// <summary>
		/// Implements the actual sending logic. Wrapped by Send to handle connection status.
		/// </summary>
		protected override bool SendFinal(string data)
		{
			byte[] bytes = StringUtils.ToBytes(data);

			m_ClientSection.Enter();

			try
			{
				m_UdpClient.Send(bytes, bytes.Length);
				PrintTx(data);
			}
			finally
			{
				m_ClientSection.Leave();
			}

			return true;
		}

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

			PrintRx(hostInfo.ToString(), data);
			Receive(data);

			if (m_ListeningRequested)
			{
				m_ClientSection.Enter();

				try
				{
					if (!m_UdpClient.Client.Connected)
						Connect();

					m_UdpClient.ReceiveAsync().ContinueWith(UdpClientReceiveHandler);
				}
				catch (SocketException)
				{
					UpdateIsConnectedState();
				}
				finally
				{
					m_ClientSection.Leave();
				}
			}

			UpdateIsConnectedState();
		}
	}
}
#endif
