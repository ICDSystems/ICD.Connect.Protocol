using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Network.Ports;
using ICD.Connect.Protocol.Network.Servers;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.Settings;
using ICD.Connect.Settings;

namespace ICD.Connect.Protocol.Network.Devices.PortServers
{
	public sealed class SerialPortServerDevice : AbstractPortServerDevice<ISerialPort, SerialPortServerDeviceSettings>
	{
		#region Fields

		private readonly ComSpecProperties m_ComSpecProperties;
		private readonly SecureNetworkProperties m_NetworkProperties;
		private readonly ConnectionStateManager m_ConnectionStateManager;

		#endregion

		#region Properties

		/// <summary>
		/// Gets the com spec properties.
		/// </summary>
		private IComSpecProperties ComSpecProperties { get { return m_ComSpecProperties; } }

		/// <summary>
		/// Gets the network properties.
		/// </summary>
		private ISecureNetworkProperties NetworkProperties { get { return m_NetworkProperties; } }

		#endregion

		public SerialPortServerDevice()
		{
			m_NetworkProperties = new SecureNetworkProperties();
			m_ComSpecProperties = new ComSpecProperties();

			m_ConnectionStateManager = new ConnectionStateManager(this)
			{
				ConfigurePort = ConfigurePort
			};
			m_ConnectionStateManager.OnIsOnlineStateChanged += CsmOnIsOnlineStateChanged;
			m_ConnectionStateManager.OnConnectedStateChanged += CsmOnConnectedStateChanged;
			m_ConnectionStateManager.OnSerialDataReceived += CsmOnSerialDataReceived;
		}

		private void ConfigurePort(IPort port)
		{
			// Com
			if (port is IComPort)
				(port as IComPort).ApplyDeviceConfiguration(ComSpecProperties);

			// Network (TCP, UDP, SSH)
			if (port is ISecureNetworkPort)
				(port as ISecureNetworkPort).ApplyDeviceConfiguration(NetworkProperties);
			else if (port is INetworkPort)
				(port as INetworkPort).ApplyDeviceConfiguration(NetworkProperties);
		}

		#region Port

		/// <summary>
		/// Sets and configures the port for communication with the physical display.
		/// </summary>
		protected override void SetPortInternal(ISerialPort port)
		{
			m_ConnectionStateManager.SetPort(port, false);
		}

		#endregion

		#region ConnectionStateManager

		/// <summary>
		/// Send received port data to all connected clients
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="stringEventArgs"></param>
		private void CsmOnSerialDataReceived(object sender, StringEventArgs stringEventArgs)
		{
			if (TcpServer != null)
				TcpServer.Send(stringEventArgs.Data);
		}

		private void CsmOnIsOnlineStateChanged(object sender, BoolEventArgs e)
		{
			UpdateCachedOnlineStatus();
		}

		private void CsmOnConnectedStateChanged(object sender, BoolEventArgs e)
		{
			UpdateCachedOnlineStatus();
		}

		#endregion

		#region TCP Server
		/// <summary>
		/// Send received server data to port
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="dataReceiveEventArgs"></param>
		protected override void IncomingServerOnDataReceived(object sender, DataReceiveEventArgs dataReceiveEventArgs)
		{
			m_ConnectionStateManager.Send(dataReceiveEventArgs.Data);
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(SerialPortServerDeviceSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Copy(m_ComSpecProperties);
			settings.Copy(m_NetworkProperties);
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			m_ComSpecProperties.ClearComSpecProperties();
			m_NetworkProperties.ClearNetworkProperties();
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(SerialPortServerDeviceSettings settings, IDeviceFactory factory)
		{

			base.ApplySettingsFinal(settings, factory);

			m_NetworkProperties.Copy(settings);
			m_ComSpecProperties.Copy(settings);
		}

		/// <summary>
		/// Override to add actions on StartSettings
		/// This should be used to start communications with devices and perform initial actions
		/// </summary>
		protected override void StartSettingsFinal()
		{
			base.StartSettingsFinal();

			m_ConnectionStateManager.Start();
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			if (m_ConnectionStateManager != null)
				yield return m_ConnectionStateManager.Port;
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		#endregion
	}
}