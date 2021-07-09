using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Protocol.Network.Devices.PortServers
{
	public abstract class AbstractPortServerDeviceSettings<T> : AbstractDeviceSettings, IPortServerDeviceSettings
		where T : IPort
	{
		private const string PORT_ELEMENT = "Port";
		private const string TCP_SERVER_PORT_ELEMENT = "TcpServerPort";

		[OriginatorIdSettingsProperty(typeof(IPort))]
		public int? Port { get; set; }

		public ushort? TcpServerPort { get; set; }

		/// <summary>
		/// Write settings elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(PORT_ELEMENT, Port == null ? null : IcdXmlConvert.ToString((int)Port));
			writer.WriteElementString(TCP_SERVER_PORT_ELEMENT, IcdXmlConvert.ToString(TcpServerPort));
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Port = XmlUtils.TryReadChildElementContentAsInt(xml, PORT_ELEMENT);
			TcpServerPort = XmlUtils.TryReadChildElementContentAsUShort(xml, TCP_SERVER_PORT_ELEMENT);
		}
	}
}