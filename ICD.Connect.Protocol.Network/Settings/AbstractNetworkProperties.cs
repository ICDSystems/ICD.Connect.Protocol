using System;
using ICD.Common.Utils.Xml;

namespace ICD.Connect.Protocol.Network.Settings
{
	public abstract class AbstractNetworkProperties : INetworkProperties
	{
		private const string ELEMENT = "Network";

		private const string NETWORK_ADDRESS_ELEMENT = "Address";
		private const string NETWORK_PORT_ELEMENT = "Port";

		#region Properties

		/// <summary>
		/// Gets/sets the configurable network address.
		/// </summary>
		public string NetworkAddress { get; set; }

		/// <summary>
		/// Gets/sets the configurable network port.
		/// </summary>
		public ushort NetworkPort { get; set; }

		#endregion

		#region Methods

		/// <summary>
		/// Clears the configured properties.
		/// </summary>
		public virtual void Clear()
		{
			NetworkAddress = null;
			NetworkPort = 0;
		}

		/// <summary>
		/// Writes the username and password configuration to xml.
		/// </summary>
		/// <param name="writer"></param>
		public void WriteElements(IcdXmlTextWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			writer.WriteStartElement(ELEMENT);
			{
				WriteInnerElements(writer);
			}
			writer.WriteEndElement();
		}

		/// <summary>
		/// Override to write additional elements to XML.
		/// </summary>
		/// <param name="writer"></param>
		protected virtual void WriteInnerElements(IcdXmlTextWriter writer)
		{
			writer.WriteElementString(NETWORK_ADDRESS_ELEMENT, NetworkAddress);
			writer.WriteElementString(NETWORK_PORT_ELEMENT, IcdXmlConvert.ToString(NetworkPort));
		}

		/// <summary>
		/// Reads the username and password configuration from xml.
		/// </summary>
		/// <param name="xml"></param>
		public void ParseXml(string xml)
		{
			Clear();

			string networking;
			if (XmlUtils.TryGetChildElementAsString(xml, ELEMENT, out networking))
				ParseInnerXml(networking);
		}

		/// <summary>
		/// Override to parse additional elements from XML.
		/// </summary>
		/// <param name="xml"></param>
		protected virtual void ParseInnerXml(string xml)
		{
			NetworkAddress = XmlUtils.TryReadChildElementContentAsString(xml, NETWORK_ADDRESS_ELEMENT);
			NetworkPort = XmlUtils.TryReadChildElementContentAsUShort(xml, NETWORK_PORT_ELEMENT) ?? 0;
		}

		#endregion
	}
}
