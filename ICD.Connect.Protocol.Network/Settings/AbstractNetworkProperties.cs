using System;
using ICD.Common.Utils.Xml;
using ICD.Connect.Settings;

namespace ICD.Connect.Protocol.Network.Settings
{
	public abstract class AbstractNetworkProperties : AbstractUsernamePasswordProperties, INetworkProperties
	{
		private const string NETWORK_ADDRESS_ELEMENT = "NetworkAddress";
		private const string NETWORK_PORT_ELEMENT = "NetworkPort";

		/// <summary>
		/// Gets/sets the configurable network address.
		/// </summary>
		public string NetworkAddress { get; set; }

		/// <summary>
		/// Gets/sets the configurable network port.
		/// </summary>
		public ushort NetworkPort { get; set; }

		/// <summary>
		/// Writes the username and password configuration to xml.
		/// </summary>
		/// <param name="writer"></param>
		public override void WriteElements(IcdXmlTextWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			writer.WriteElementString(NETWORK_ADDRESS_ELEMENT, NetworkAddress);
			writer.WriteElementString(NETWORK_PORT_ELEMENT, IcdXmlConvert.ToString(NetworkPort));

			base.WriteElements(writer);
		}

		/// <summary>
		/// Reads the username and password configuration from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			NetworkAddress = XmlUtils.TryReadChildElementContentAsString(xml, NETWORK_ADDRESS_ELEMENT);
			NetworkPort = XmlUtils.TryReadChildElementContentAsUShort(xml, NETWORK_PORT_ELEMENT) ?? 0;

			base.ParseXml(xml);
		}
	}
}
