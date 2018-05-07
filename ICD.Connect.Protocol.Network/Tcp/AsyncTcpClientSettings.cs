using ICD.Common.Utils.Xml;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.Network.Tcp
{
	[KrangSettings("TCP", typeof(AsyncTcpClient))]
	public sealed class AsyncTcpClientSettings : AbstractSerialPortSettings
	{
		private const string ADDRESS_ELEMENT = "Address";
		private const string HOST_PORT_ELEMENT = "Port";
		private const string BUFFER_SIZE_ELEMENT = "BufferSize";

		private ushort m_BufferSize = AsyncTcpClient.DEFAULT_BUFFER_SIZE;

		#region Properties

		public string Address { get; set; }

		public ushort Port { get; set; }

		public ushort BufferSize { get { return m_BufferSize; } set { m_BufferSize = value; } }

		#endregion

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

			Address = XmlUtils.TryReadChildElementContentAsString(xml, ADDRESS_ELEMENT);
			Port = XmlUtils.TryReadChildElementContentAsUShort(xml, HOST_PORT_ELEMENT) ?? 0;
			BufferSize = XmlUtils.TryReadChildElementContentAsUShort(xml, BUFFER_SIZE_ELEMENT) ??
			             AsyncTcpClient.DEFAULT_BUFFER_SIZE;
		}
	}
}
