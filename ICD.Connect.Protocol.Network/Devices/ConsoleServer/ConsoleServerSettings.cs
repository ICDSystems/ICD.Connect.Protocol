using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.Network.Devices.ConsoleServer
{
	[KrangSettings("ConsoleServer", typeof(ConsoleServerDevice))]
	public sealed class ConsoleServerSettings : AbstractDeviceSettings
	{

		private const string PORT_ELEMENT = "Port";
		private const string PORT_NUMBER_ELEMENT = "PortNumber";
		private const string MAX_CLIENTS_ELEMENT = "MaxClients";
		public const ushort DEFAULT_PORT = 8023;
		public const int DEFAULT_MAX_CLIENTS = 32;

		public ushort Port { get; set; }

		public int MaxClients { get; set; }

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Port = XmlUtils.TryReadChildElementContentAsUShort(xml, PORT_ELEMENT) ??
			       XmlUtils.TryReadChildElementContentAsUShort(xml, PORT_NUMBER_ELEMENT) ??
			       DEFAULT_PORT;

			MaxClients = XmlUtils.TryReadChildElementContentAsInt(xml, MAX_CLIENTS_ELEMENT) ?? DEFAULT_MAX_CLIENTS;
		}

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(PORT_ELEMENT, IcdXmlConvert.ToString(Port));
			writer.WriteElementString(MAX_CLIENTS_ELEMENT, IcdXmlConvert.ToString(MaxClients));
		}
	}
}
