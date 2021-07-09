using ICD.Connect.Protocol.Network.Servers;
using ICD.Connect.Protocol.Ports.IrPort;
using ICD.Connect.Protocol.Settings;
using ICD.Connect.Settings;

namespace ICD.Connect.Protocol.Network.Devices.PortServers
{
	public sealed class IrPortServerDevice : AbstractPortServerDevice<IIrPort, IrPortServerDeviceSettings>
	{

		private readonly IrDriverProperties m_IrDriverProperties;

		public IrPortServerDevice()
		{
			m_IrDriverProperties = new IrDriverProperties();
		}

		/// <summary>
		/// Called after the port is subscribed to
		/// Run any action needed to set the port here
		/// </summary>
		/// <param name="port"></param>
		protected override void SetPortInternal(IIrPort port)
		{
			base.SetPortInternal(port);

			ConfigurePort(port);
		}

		/// <summary>
		/// Configures the given port for communication with the device.
		/// </summary>
		/// <param name="port"></param>
		private void ConfigurePort(IIrPort port)
		{
			// IR
			if (port != null)
				port.ApplyDeviceConfiguration(m_IrDriverProperties);
		}

		#region TCPServer Callbacks

		protected override void IncomingServerOnDataReceived(object sender, DataReceiveEventArgs dataReceiveEventArgs)
		{
			throw new System.NotImplementedException();
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(IrPortServerDeviceSettings settings)
		{
			base.CopySettingsFinal(settings);
			
			settings.Copy(m_IrDriverProperties);
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			m_IrDriverProperties.ClearIrProperties();
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(IrPortServerDeviceSettings settings, IDeviceFactory factory)
		{
			// grab IR driver properties first so port gets configured when set
			m_IrDriverProperties.Copy(settings);

			base.ApplySettingsFinal(settings, factory);
		}

		#endregion
	}
}
