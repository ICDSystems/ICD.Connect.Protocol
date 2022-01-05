using System;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Protocol.Network.EventArguments;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings;

namespace ICD.Connect.Protocol.Network.Ports.Udp
{
	public sealed class IcdUdpClient : AbstractNetworkPort<IcdUdpClientSettings>
	{
		public const string ACCEPT_ALL = "0.0.0.0";

		private readonly NetworkProperties m_NetworkProperties;

		private bool m_ListeningRequested;

		[CanBeNull]
		private IcdUdpSocket m_UdpSocket;

		#region Properties

		/// <summary>
		/// Gets the Network configuration properties.
		/// </summary>
		public override INetworkProperties NetworkProperties { get { return m_NetworkProperties; } }

		/// <summary>
		/// Address to accept connections from.
		/// </summary>
		[PublicAPI]
		public override string Address { get; set; }

		/// <summary>
		/// Port to accept connections from.
		/// </summary>
		[PublicAPI]
		public override ushort Port { get; set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public IcdUdpClient()
		{
			m_NetworkProperties = new NetworkProperties();

			Address = ACCEPT_ALL;

			IcdEnvironment.OnEthernetEvent += IcdEnvironmentOnEthernetEvent;
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			IcdEnvironment.OnEthernetEvent -= IcdEnvironmentOnEthernetEvent;

			base.DisposeFinal(disposing);

			Disconnect();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Connects to the end point.
		/// </summary>
		public override void Connect()
		{
			ushort port = Port;
			string address = Address;

			try
			{
				Unsubscribe(m_UdpSocket);
				if (m_UdpSocket != null)
					m_UdpSocket.Dispose();
				m_UdpSocket = new IcdUdpSocket(address, port);
				Subscribe(m_UdpSocket);
			}
			catch (Exception e)
			{
				Logger.Log(eSeverity.Error, "Failed to connect - {0}", e.Message);
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

			if (m_UdpSocket != null)
			{
				Unsubscribe(m_UdpSocket);
				m_UdpSocket.Dispose();
			}
			m_UdpSocket = null;

			UpdateIsConnectedState();
		}

		/// <summary>
		/// Returns the connection state of the port
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsConnectedState()
		{
			return m_UdpSocket != null && m_UdpSocket.IsConnected;
		}

		/// <summary>
		/// Implements the actual sending logic. Wrapped by Send to handle connection status.
		/// </summary>
		protected override bool SendFinal(string data)
		{
			if (Address != ACCEPT_ALL)
				return SendToAddress(data, Address, Port);

			if (m_UdpSocket == null)
			{
				Logger.Log(eSeverity.Error, "Failed to send data - Wrapped client is null");
				return false;
			}

			try
			{
				m_UdpSocket.Send(data);
				PrintTx(() => data);
				return true;
			}
			catch (Exception e)
			{
				Logger.Log(eSeverity.Error, "Failed to send data to {0} - {1}", Port, e.Message);
			}
			finally
			{
				UpdateIsConnectedState();
			}

			return false;
		}

		/// <summary>
		/// Implements the actual sending logic. Wrapped by SendToAddress to handle connection status.
		/// </summary>
		public bool SendToAddress(string data, string ipAddress, int port)
		{
			if (m_UdpSocket == null)
			{
				Logger.Log(eSeverity.Error, "Failed to send data to {0}:{1} - Wrapped client is null",
				           ipAddress, port);
				return false;
			}

			try
			{
				m_UdpSocket.SendToAddress(data, ipAddress, port);
				PrintTx(new HostInfo(ipAddress, (ushort)port).ToString(), () => data);
				return true;
			}
			catch (Exception e)
			{
				Logger.Log(eSeverity.Error, "Failed to send data to {0}:{1} - {2}",
				           ipAddress, port, e.Message);
			}
			finally
			{
				UpdateIsConnectedState();
			}

			return false;
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Handle Ethernet events
		/// </summary>
		/// <param name="adapter"></param>
		/// <param name="type"></param>
		private void IcdEnvironmentOnEthernetEvent(IcdEnvironment.eEthernetAdapterType adapter,
		                                           IcdEnvironment.eEthernetEventType type)
		{
			switch (type)
			{
				case IcdEnvironment.eEthernetEventType.LinkUp:
					if (m_ListeningRequested &&
						m_UdpSocket != null &&
						!m_UdpSocket.IsConnected)
						Connect();
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			UpdateIsConnectedState();
		}

		#endregion

		#region UdpSocket Callbacks

		/// <summary>
		/// Subscribe to the UDP socket events.
		/// </summary>
		/// <param name="udpSocket"></param>
		private void Subscribe([CanBeNull] IcdUdpSocket udpSocket)
		{
			if (udpSocket == null)
				return;

			udpSocket.OnIsConnectedStateChanged += UdpSocketOnIsConnectedStateChanged;
			udpSocket.OnDataReceived += UdpSocketOnDataReceived;
		}

		/// <summary>
		/// Unsubscribe from the UDP socket events.
		/// </summary>
		/// <param name="udpSocket"></param>
		private void Unsubscribe([CanBeNull] IcdUdpSocket udpSocket)
		{
			if (udpSocket == null)
				return;

			udpSocket.OnIsConnectedStateChanged -= UdpSocketOnIsConnectedStateChanged;
			udpSocket.OnDataReceived -= UdpSocketOnDataReceived;
		}

		private void UdpSocketOnIsConnectedStateChanged(object sender, BoolEventArgs eventArgs)
		{
			UpdateIsConnectedState();
		}

		private void UdpSocketOnDataReceived(object sender, UdpDataReceivedEventArgs eventArgs)
		{
			try
			{
				HostInfo host = eventArgs.Host;
				string data = eventArgs.Data;

				// Ignore messages that aren't from the configured endpoint
				if (Address != ACCEPT_ALL && Address != host.Address)
					return;

				PrintRx(host.ToString(), () => data);
				Receive(data);
			}
			finally
			{
				UpdateIsConnectedState();
			}
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			ApplyConfiguration();
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(IcdUdpClientSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			ApplyConfiguration();
		}

		#endregion
	}
}
