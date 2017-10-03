using System;
using ICD.Common.Properties;
using ICD.Common.Utils.Xml;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.Network.Udp
{
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
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlFactoryMethod(FACTORY_NAME)]
		public static AsyncUdpClientSettings FromXml(string xml)
		{
			string address = XmlUtils.ReadChildElementContentAsString(xml, ADDRESS_ELEMENT);
			ushort port = (ushort)XmlUtils.ReadChildElementContentAsInt(xml, HOST_PORT_ELEMENT);
			ushort bufferSize = (ushort)XmlUtils.ReadChildElementContentAsInt(xml, BUFFER_SIZE_ELEMENT);

			AsyncUdpClientSettings output = new AsyncUdpClientSettings
			{
				Address = address,
				Port = port,
				BufferSize = bufferSize
			};

			ParseXml(output, xml);
			return output;
		}
	}
}
