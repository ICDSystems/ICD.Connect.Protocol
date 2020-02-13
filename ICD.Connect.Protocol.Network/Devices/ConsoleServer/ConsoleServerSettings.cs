using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.Network.Devices.ConsoleServer
{
	[KrangSettings(FACTORY_NAME, typeof(ConsoleServerDevice))]
	[KrangSettings("IcdConsoleServer", typeof(ConsoleServerDevice))]
	public sealed class ConsoleServerSettings : AbstractDeviceSettings
	{
		private const string FACTORY_NAME = "ConsoleServer";

		private const string PORT_ELEMENT = "Port";
		private const string PORT_NUMBER_ELEMENT = "PortNumber";
		public const ushort DEFAULT_PORT = 8023;

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		public ushort Port { get; set; }

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
		}

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(PORT_ELEMENT, IcdXmlConvert.ToString(Port));
		}
	}
}
