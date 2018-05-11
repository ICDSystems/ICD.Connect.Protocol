using System;
using ICD.Common.Utils.Xml;
using ICD.Connect.Protocol.Ports.ComPort;

namespace ICD.Connect.Protocol.Settings
{
	public static class ComSpecSettingsParsing
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
		/// Writes the comspec configuration to xml.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="instance"></param>
		public static void WriteElements(IcdXmlTextWriter writer, IComSpecSettings instance)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			if (instance == null)
				throw new ArgumentNullException("instance");

			writer.WriteElementString(COM_SPEC_BAUD_RATE_ELEMENT, IcdXmlConvert.ToString(instance.ComSpecBaudRate));
			writer.WriteElementString(COM_SPEC_NUMBER_OF_DATA_BITS_ELEMENT, IcdXmlConvert.ToString(instance.ComSpecNumberOfDataBits));
			writer.WriteElementString(COM_SPEC_PARITY_TYPE_ELEMENT, IcdXmlConvert.ToString(instance.ComSpecParityType));
			writer.WriteElementString(COM_SPEC_NUMBER_OF_STOP_BITS_ELEMENT, IcdXmlConvert.ToString(instance.ComSpecNumberOfStopBits));
			writer.WriteElementString(COM_SPEC_PROTOCOL_TYPE_ELEMENT, IcdXmlConvert.ToString(instance.ComSpecProtocolType));
			writer.WriteElementString(COM_SPEC_HARDWARE_HAND_SHAKE_ELEMENT, IcdXmlConvert.ToString(instance.ComSpecHardwareHandShake));
			writer.WriteElementString(COM_SPEC_SOFTWARE_HANDSHAKE_ELEMENT, IcdXmlConvert.ToString(instance.ComSpecSoftwareHandshake));
			writer.WriteElementString(COM_SPEC_REPORT_CTS_CHANGES_ELEMENT, IcdXmlConvert.ToString(instance.ComSpecReportCtsChanges));
		}

		/// <summary>
		/// Reads the comspec configuration from xml.
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="instance"></param>
		public static void ParseXml(string xml, IComSpecSettings instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			instance.ComSpecBaudRate = XmlUtils.TryReadChildElementContentAsEnum<eComBaudRates>(xml, COM_SPEC_BAUD_RATE_ELEMENT, true) ?? default(eComBaudRates);
			instance.ComSpecNumberOfDataBits = XmlUtils.TryReadChildElementContentAsEnum<eComDataBits>(xml, COM_SPEC_NUMBER_OF_DATA_BITS_ELEMENT, true) ?? default(eComDataBits);
			instance.ComSpecParityType = XmlUtils.TryReadChildElementContentAsEnum<eComParityType>(xml, COM_SPEC_PARITY_TYPE_ELEMENT, true) ?? default(eComParityType);
			instance.ComSpecNumberOfStopBits = XmlUtils.TryReadChildElementContentAsEnum<eComStopBits>(xml, COM_SPEC_NUMBER_OF_STOP_BITS_ELEMENT, true) ?? default(eComStopBits);
			instance.ComSpecProtocolType = XmlUtils.TryReadChildElementContentAsEnum<eComProtocolType>(xml, COM_SPEC_PROTOCOL_TYPE_ELEMENT, true) ?? default(eComProtocolType);
			instance.ComSpecHardwareHandShake = XmlUtils.TryReadChildElementContentAsEnum<eComHardwareHandshakeType>(xml, COM_SPEC_HARDWARE_HAND_SHAKE_ELEMENT, true) ?? default(eComHardwareHandshakeType);
			instance.ComSpecSoftwareHandshake = XmlUtils.TryReadChildElementContentAsEnum<eComSoftwareHandshakeType>(xml, COM_SPEC_SOFTWARE_HANDSHAKE_ELEMENT, true) ?? default(eComSoftwareHandshakeType);
			instance.ComSpecReportCtsChanges = XmlUtils.TryReadChildElementContentAsBoolean(xml, COM_SPEC_REPORT_CTS_CHANGES_ELEMENT) ?? false;
		}
	}
}
