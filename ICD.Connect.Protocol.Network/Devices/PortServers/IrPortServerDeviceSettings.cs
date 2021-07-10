using ICD.Common.Utils.Xml;
using ICD.Connect.Protocol.Ports.IrPort;
using ICD.Connect.Protocol.Settings;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.Network.Devices.PortServers
{
	[KrangSettings("IrPortServerDevice", typeof(IrPortServerDevice))]
	public sealed class IrPortServerDeviceSettings : AbstractPortServerDeviceSettings<IIrPort>, IIrDriverSettings
	{

		private readonly IrDriverProperties m_IrDriverProperties;

		#region IR Driver

		/// <summary>
		/// Gets/sets the configurable path to the IR driver.
		/// </summary>
		public string IrDriverPath
		{
			get { return m_IrDriverProperties.IrDriverPath; }
			set { m_IrDriverProperties.IrDriverPath = value; }
		}

		/// <summary>
		/// Gets/sets the configurable pulse time for the IR driver.
		/// </summary>
		public ushort? IrPulseTime
		{
			get { return m_IrDriverProperties.IrPulseTime; }
			set { m_IrDriverProperties.IrPulseTime = value; }
		}

		/// <summary>
		/// Gets/sets the configurable between time for the IR driver.
		/// </summary>
		public ushort? IrBetweenTime
		{
			get { return m_IrDriverProperties.IrBetweenTime; }
			set { m_IrDriverProperties.IrBetweenTime = value; }
		}

		/// <summary>
		/// Clears the configured values.
		/// </summary>
		void IIrDriverProperties.ClearIrProperties()
		{
			m_IrDriverProperties.ClearIrProperties();
		}

		#endregion

		public IrPortServerDeviceSettings()
		{
			m_IrDriverProperties = new IrDriverProperties();
		}

		#region Xml

		/// <summary>
		/// Write settings elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			m_IrDriverProperties.WriteElements(writer);
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			m_IrDriverProperties.ParseXml(xml);
		}

		#endregion
	}
}
