using System;
using ICD.Common.Utils.Xml;
using ICD.Connect.Settings;

namespace ICD.Connect.Protocol.Network.Settings
{
	public static class NetworkSettingsParsing
	{
		private const string NETWORK_ADDRESS_ELEMENT = "NetworkAddress";
		private const string NETWORK_PORT_ELEMENT = "NetworkPort";

		/// <summary>
		/// Writes the username and password configuration to xml.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="instance"></param>
		public static void WriteElements(IcdXmlTextWriter writer, INetworkSettings instance)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			if (instance == null)
				throw new ArgumentNullException("instance");

			writer.WriteElementString(NETWORK_ADDRESS_ELEMENT, instance.NetworkAddress);
			writer.WriteElementString(NETWORK_PORT_ELEMENT, IcdXmlConvert.ToString(instance.NetworkPort));

			UsernamePasswordSettingsParsing.WriteElements(writer, instance);
		}

		/// <summary>
		/// Reads the username and password configuration from xml.
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="instance"></param>
		public static void ParseXml(string xml, INetworkSettings instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			instance.NetworkAddress = XmlUtils.TryReadChildElementContentAsString(xml, NETWORK_ADDRESS_ELEMENT);
			instance.NetworkPort = XmlUtils.TryReadChildElementContentAsUShort(xml, NETWORK_PORT_ELEMENT) ?? 0;

			UsernamePasswordSettingsParsing.ParseXml(xml, instance);
		}
	}
}
