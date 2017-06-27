using System;
using ICD.Common.Properties;
using ICD.Common.Utils.Xml;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Attributes.Factories;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Protocol.Network.Tcp
{
	public sealed class AsyncTcpClientSettings : AbstractPortSettings
	{
		private const string FACTORY_NAME = "TCP";

		private const string ADDRESS_ELEMENT = "Address";
		private const string HOST_PORT_ELEMENT = "Port";
		private const string BUFFER_SIZE_ELEMENT = "BufferSize";

		private ushort m_BufferSize = AsyncTcpClient.DEFAULT_BUFFER_SIZE;

		#region Properties

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		/// <summary>
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(AsyncTcpClient); } }

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
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlPortSettingsFactoryMethod(FACTORY_NAME)]
		public static AsyncTcpClientSettings FromXml(string xml)
		{
			string address = XmlUtils.ReadChildElementContentAsString(xml, ADDRESS_ELEMENT);
			ushort port = (ushort)XmlUtils.ReadChildElementContentAsInt(xml, HOST_PORT_ELEMENT);
			ushort bufferSize = (ushort)XmlUtils.ReadChildElementContentAsInt(xml, BUFFER_SIZE_ELEMENT);

			AsyncTcpClientSettings output = new AsyncTcpClientSettings
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
