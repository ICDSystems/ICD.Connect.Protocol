using System;
using ICD.Common.Utils.Xml;
using ICD.Connect.Protocol.Ports.ComPort;

namespace ICD.Connect.Protocol.Settings
{
	public abstract class AbstractComSpecProperties : IComSpecProperties
	{
		const string COM_SPEC_BAUD_RATE_ELEMENT = "ComSpecBaudRate";
		const string COM_SPEC_NUMBER_OF_DATA_BITS_ELEMENT = "ComSpecNumberOfDataBits";
		const string COM_SPEC_PARITY_TYPE_ELEMENT = "ComSpecParityType";
		const string COM_SPEC_NUMBER_OF_STOP_BITS_ELEMENT = "ComSpecNumberOfStopBits";
		const string COM_SPEC_PROTOCOL_TYPE_ELEMENT = "ComSpecProtocolType";
		const string COM_SPEC_HARDWARE_HAND_SHAKE_ELEMENT = "ComSpecHardwareHandShake";
		const string COM_SPEC_SOFTWARE_HANDSHAKE_ELEMENT = "ComSpecSoftwareHandshake";
		const string COM_SPEC_REPORT_CTS_CHANGES_ELEMENT = "ComSpecReportCtsChanges";

		/// <summary>
		/// Gets/sets the configurable baud rate.
		/// </summary>
		public eComBaudRates ComSpecBaudRate { get; set; }

		/// <summary>
		/// Gets/sets the configurable number of data bits.
		/// </summary>
		public eComDataBits ComSpecNumberOfDataBits { get; set; }

		/// <summary>
		/// Gets/sets the configurable parity type.
		/// </summary>
		public eComParityType ComSpecParityType { get; set; }

		/// <summary>
		/// Gets/sets the configurable number of stop bits.
		/// </summary>
		public eComStopBits ComSpecNumberOfStopBits { get; set; }

		/// <summary>
		/// Gets/sets the configurable protocol type.
		/// </summary>
		public eComProtocolType ComSpecProtocolType { get; set; }

		/// <summary>
		/// Gets/sets the configurable hardware handshake type.
		/// </summary>
		public eComHardwareHandshakeType ComSpecHardwareHandShake { get; set; }

		/// <summary>
		/// Gets/sets the configurable software handshake type.
		/// </summary>
		public eComSoftwareHandshakeType ComSpecSoftwareHandshake { get; set; }

		/// <summary>
		/// Gets/sets the configurable report CTS changes state.
		/// </summary>
		public bool ComSpecReportCtsChanges { get; set; }

		/// <summary>
		/// Writes the comspec configuration to xml.
		/// </summary>
		/// <param name="writer"></param>
		public void WriteElements(IcdXmlTextWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			writer.WriteElementString(COM_SPEC_BAUD_RATE_ELEMENT, IcdXmlConvert.ToString(ComSpecBaudRate));
			writer.WriteElementString(COM_SPEC_NUMBER_OF_DATA_BITS_ELEMENT, IcdXmlConvert.ToString(ComSpecNumberOfDataBits));
			writer.WriteElementString(COM_SPEC_PARITY_TYPE_ELEMENT, IcdXmlConvert.ToString(ComSpecParityType));
			writer.WriteElementString(COM_SPEC_NUMBER_OF_STOP_BITS_ELEMENT, IcdXmlConvert.ToString(ComSpecNumberOfStopBits));
			writer.WriteElementString(COM_SPEC_PROTOCOL_TYPE_ELEMENT, IcdXmlConvert.ToString(ComSpecProtocolType));
			writer.WriteElementString(COM_SPEC_HARDWARE_HAND_SHAKE_ELEMENT, IcdXmlConvert.ToString(ComSpecHardwareHandShake));
			writer.WriteElementString(COM_SPEC_SOFTWARE_HANDSHAKE_ELEMENT, IcdXmlConvert.ToString(ComSpecSoftwareHandshake));
			writer.WriteElementString(COM_SPEC_REPORT_CTS_CHANGES_ELEMENT, IcdXmlConvert.ToString(ComSpecReportCtsChanges));
		}

		/// <summary>
		/// Reads the comspec configuration from xml.
		/// </summary>
		/// <param name="xml"></param>
		public void ParseXml(string xml)
		{
			ComSpecBaudRate = XmlUtils.TryReadChildElementContentAsEnum<eComBaudRates>(xml, COM_SPEC_BAUD_RATE_ELEMENT, true) ?? default(eComBaudRates);
			ComSpecNumberOfDataBits = XmlUtils.TryReadChildElementContentAsEnum<eComDataBits>(xml, COM_SPEC_NUMBER_OF_DATA_BITS_ELEMENT, true) ?? default(eComDataBits);
			ComSpecParityType = XmlUtils.TryReadChildElementContentAsEnum<eComParityType>(xml, COM_SPEC_PARITY_TYPE_ELEMENT, true) ?? default(eComParityType);
			ComSpecNumberOfStopBits = XmlUtils.TryReadChildElementContentAsEnum<eComStopBits>(xml, COM_SPEC_NUMBER_OF_STOP_BITS_ELEMENT, true) ?? default(eComStopBits);
			ComSpecProtocolType = XmlUtils.TryReadChildElementContentAsEnum<eComProtocolType>(xml, COM_SPEC_PROTOCOL_TYPE_ELEMENT, true) ?? default(eComProtocolType);
			ComSpecHardwareHandShake = XmlUtils.TryReadChildElementContentAsEnum<eComHardwareHandshakeType>(xml, COM_SPEC_HARDWARE_HAND_SHAKE_ELEMENT, true) ?? default(eComHardwareHandshakeType);
			ComSpecSoftwareHandshake = XmlUtils.TryReadChildElementContentAsEnum<eComSoftwareHandshakeType>(xml, COM_SPEC_SOFTWARE_HANDSHAKE_ELEMENT, true) ?? default(eComSoftwareHandshakeType);
			ComSpecReportCtsChanges = XmlUtils.TryReadChildElementContentAsBoolean(xml, COM_SPEC_REPORT_CTS_CHANGES_ELEMENT) ?? false;
		}
	}
}
