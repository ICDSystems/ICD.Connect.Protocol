using System;
using ICD.Common.Utils.Xml;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.Network.Udp
{
	[KrangSettings(FACTORY_NAME)]
	public sealed class AsyncUdpClientSettings : AbstractSerialPortSettings
	{
		private const string FACTORY_NAME = "UDP";

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		/// <summary>
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(AsyncUdpClient); } }

		private const string ADDRESS_ELEMENT = "Address";
		private const string HOST_PORT_ELEMENT = "Port";
		private const string BUFFER_SIZE_ELEMENT = "BufferSize";

		private int m_BufferSize = AsyncUdpClient.DEFAULT_BUFFER_SIZE;

		public string Address { get; set; }
		public ushort Port { get; set; }
		public int BufferSize { get { return m_BufferSize; } set { m_BufferSize = value; } }

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(ADDRESS_ELEMENT, Address);
			writer.WriteElementString(HOST_PORT_ELEMENT, IcdXmlConvert.ToString(Port));
			writer.WriteElementString(BUFFER_SIZE_ELEMENT, IcdXmlConvert.ToString(BufferSize));
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			string address = XmlUtils.TryReadChildElementContentAsString(xml, ADDRESS_ELEMENT);
			ushort port = XmlUtils.TryReadChildElementContentAsUShort(xml, HOST_PORT_ELEMENT) ?? 0;
			ushort bufferSize = XmlUtils.TryReadChildElementContentAsUShort(xml, BUFFER_SIZE_ELEMENT) ?? 0;

			Address = address;
			Port = port;
			BufferSize = bufferSize;
		}
	}
}
