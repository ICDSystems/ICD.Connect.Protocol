using ICD.Common.Utils.Xml;

namespace ICD.Connect.Protocol.Ports.IoPort
{
	public abstract class AbstractIoPortSettings : AbstractPortSettings, IIoPortSettings
	{
		private const string CONFIGURATION_ELEMENT = "Configuration";

		/// <summary>
		/// Gets/sets the port configuration.
		/// </summary>
		public eIoPortConfiguration Configuration { get; set; }

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(CONFIGURATION_ELEMENT, IcdXmlConvert.ToString(Configuration));
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Configuration = XmlUtils.TryReadChildElementContentAsEnum<eIoPortConfiguration>(xml, CONFIGURATION_ELEMENT, true) ??
							eIoPortConfiguration.None;
		}
	}
}
