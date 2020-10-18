#if !SIMPLSHARP
using ICD.Common.Utils.Xml;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.Ports.ComPort
{
	[KrangSettings("SerialPort", typeof(SerialPortAdapter))]
	public sealed class SerialPortAdapterSettings : AbstractComPortSettings
	{
		private const string ELEMENT_SERIAL_LINE = "SerialLine";
		public const string DEFAULT_SERIAL_LINE = "COM1";

		/// <summary>
		/// The name of the serial port (e.g. "COM1")
		/// </summary>
		public string SerialLine { get; set; }

		/// <summary>
		/// Constructor.
		/// </summary>
		public SerialPortAdapterSettings()
		{
			SerialLine = DEFAULT_SERIAL_LINE;
		}

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(ELEMENT_SERIAL_LINE, SerialLine);
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			SerialLine = XmlUtils.TryReadChildElementContentAsString(xml, ELEMENT_SERIAL_LINE)
			             ?? DEFAULT_SERIAL_LINE;
		}
	}
}
#endif
