using System;
using ICD.Common.Utils.Xml;
using ICD.Connect.Protocol.Ports.ComPort;

namespace ICD.Connect.Protocol.Settings
{
	public abstract class AbstractComSpecProperties : IComSpecProperties
	{
		private const string ELEMENT = "ComSpec";

		private const string COM_SPEC_BAUD_RATE_ELEMENT = "BaudRate";
		private const string COM_SPEC_NUMBER_OF_DATA_BITS_ELEMENT = "NumberOfDataBits";
		private const string COM_SPEC_PARITY_TYPE_ELEMENT = "ParityType";
		private const string COM_SPEC_NUMBER_OF_STOP_BITS_ELEMENT = "NumberOfStopBits";
		private const string COM_SPEC_PROTOCOL_TYPE_ELEMENT = "ProtocolType";
		private const string COM_SPEC_HARDWARE_HAND_SHAKE_ELEMENT = "HardwareHandShake";
		private const string COM_SPEC_SOFTWARE_HANDSHAKE_ELEMENT = "SoftwareHandshake";
		private const string COM_SPEC_REPORT_CTS_CHANGES_ELEMENT = "ReportCtsChanges";

		#region Properties

		/// <summary>
		/// Gets/sets the configurable baud rate.
		/// </summary>
		public eComBaudRates? ComSpecBaudRate { get; set; }

		/// <summary>
		/// Gets/sets the configurable number of data bits.
		/// </summary>
		public eComDataBits? ComSpecNumberOfDataBits { get; set; }

		/// <summary>
		/// Gets/sets the configurable parity type.
		/// </summary>
		public eComParityType? ComSpecParityType { get; set; }

		/// <summary>
		/// Gets/sets the configurable number of stop bits.
		/// </summary>
		public eComStopBits? ComSpecNumberOfStopBits { get; set; }

		/// <summary>
		/// Gets/sets the configurable protocol type.
		/// </summary>
		public eComProtocolType? ComSpecProtocolType { get; set; }

		/// <summary>
		/// Gets/sets the configurable hardware handshake type.
		/// </summary>
		public eComHardwareHandshakeType? ComSpecHardwareHandShake { get; set; }

		/// <summary>
		/// Gets/sets the configurable software handshake type.
		/// </summary>
		public eComSoftwareHandshakeType? ComSpecSoftwareHandshake { get; set; }

		/// <summary>
		/// Gets/sets the configurable report CTS changes state.
		/// </summary>
		public bool? ComSpecReportCtsChanges { get; set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractComSpecProperties()
		{
			Clear();
		}

		#region Methods

		/// <summary>
		/// Clears the configured properties.
		/// </summary>
		public void Clear()
		{
			ComSpecBaudRate = null;
			ComSpecNumberOfDataBits = null;
			ComSpecParityType = null;
			ComSpecNumberOfStopBits = null;
			ComSpecProtocolType = null;
			ComSpecHardwareHandShake = null;
			ComSpecSoftwareHandshake = null;
			ComSpecReportCtsChanges = null;
		}

		/// <summary>
		/// Writes the comspec configuration to xml.
		/// </summary>
		/// <param name="writer"></param>
		public void WriteElements(IcdXmlTextWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			writer.WriteStartElement(ELEMENT);
			{
				writer.WriteElementString(COM_SPEC_BAUD_RATE_ELEMENT, IcdXmlConvert.ToString(ComSpecBaudRate));
				writer.WriteElementString(COM_SPEC_NUMBER_OF_DATA_BITS_ELEMENT, IcdXmlConvert.ToString(ComSpecNumberOfDataBits));
				writer.WriteElementString(COM_SPEC_PARITY_TYPE_ELEMENT, IcdXmlConvert.ToString(ComSpecParityType));
				writer.WriteElementString(COM_SPEC_NUMBER_OF_STOP_BITS_ELEMENT, IcdXmlConvert.ToString(ComSpecNumberOfStopBits));
				writer.WriteElementString(COM_SPEC_PROTOCOL_TYPE_ELEMENT, IcdXmlConvert.ToString(ComSpecProtocolType));
				writer.WriteElementString(COM_SPEC_HARDWARE_HAND_SHAKE_ELEMENT, IcdXmlConvert.ToString(ComSpecHardwareHandShake));
				writer.WriteElementString(COM_SPEC_SOFTWARE_HANDSHAKE_ELEMENT, IcdXmlConvert.ToString(ComSpecSoftwareHandshake));
				writer.WriteElementString(COM_SPEC_REPORT_CTS_CHANGES_ELEMENT, IcdXmlConvert.ToString(ComSpecReportCtsChanges));
			}
			writer.WriteEndElement();
		}

		/// <summary>
		/// Reads the comspec configuration from xml.
		/// </summary>
		/// <param name="xml"></param>
		public void ParseXml(string xml)
		{
			Clear();

			string comSpec;
			if (!XmlUtils.TryGetChildElementAsString(xml, ELEMENT, out comSpec))
				return;

			ComSpecBaudRate = XmlUtils.TryReadChildElementContentAsEnum<eComBaudRates>(comSpec, COM_SPEC_BAUD_RATE_ELEMENT, true);
			ComSpecNumberOfDataBits = XmlUtils.TryReadChildElementContentAsEnum<eComDataBits>(comSpec, COM_SPEC_NUMBER_OF_DATA_BITS_ELEMENT, true);
			ComSpecParityType = XmlUtils.TryReadChildElementContentAsEnum<eComParityType>(comSpec, COM_SPEC_PARITY_TYPE_ELEMENT, true);
			ComSpecNumberOfStopBits = XmlUtils.TryReadChildElementContentAsEnum<eComStopBits>(comSpec, COM_SPEC_NUMBER_OF_STOP_BITS_ELEMENT, true);
			ComSpecProtocolType = XmlUtils.TryReadChildElementContentAsEnum<eComProtocolType>(comSpec, COM_SPEC_PROTOCOL_TYPE_ELEMENT, true);
			ComSpecHardwareHandShake = XmlUtils.TryReadChildElementContentAsEnum<eComHardwareHandshakeType>(comSpec, COM_SPEC_HARDWARE_HAND_SHAKE_ELEMENT, true);
			ComSpecSoftwareHandshake = XmlUtils.TryReadChildElementContentAsEnum<eComSoftwareHandshakeType>(comSpec, COM_SPEC_SOFTWARE_HANDSHAKE_ELEMENT, true);
			ComSpecReportCtsChanges = XmlUtils.TryReadChildElementContentAsBoolean(comSpec, COM_SPEC_REPORT_CTS_CHANGES_ELEMENT);
		}

		#endregion
	}
}
