using System;
using ICD.Common.Utils.Xml;

namespace ICD.Connect.Protocol.Settings
{
	public static class IrDriverSettingsParsing
	{
		public const ushort DEFAULT_PULSE_TIME = 100;
		public const ushort DEFAULT_BETWEEN_TIME = 750;

		private const string DRIVER_ELEMENT = "Driver";
		private const string PULSETIME_ELEMENT = "PulseTime";
		private const string BETWEENTIME_ELEMENT = "BetweenTime";

		/// <summary>
		/// Writes the IR configuration to xml.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="instance"></param>
		public static void WriteElements(IcdXmlTextWriter writer, IIrDriverSettings instance)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			if (instance == null)
				throw new ArgumentNullException("instance");

			writer.WriteElementString(DRIVER_ELEMENT, instance.IrDriverPath);
			writer.WriteElementString(PULSETIME_ELEMENT, IcdXmlConvert.ToString(instance.IrPulseTime));
			writer.WriteElementString(BETWEENTIME_ELEMENT, IcdXmlConvert.ToString(instance.IrBetweenTime));
		}

		/// <summary>
		/// Reads the IR configuration from xml.
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="instance"></param>
		public static void ParseXml(string xml, IIrDriverSettings instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			instance.IrDriverPath = XmlUtils.TryReadChildElementContentAsString(xml, DRIVER_ELEMENT);
			instance.IrPulseTime = XmlUtils.TryReadChildElementContentAsUShort(xml, PULSETIME_ELEMENT) ?? DEFAULT_PULSE_TIME;
			instance.IrBetweenTime = XmlUtils.TryReadChildElementContentAsUShort(xml, BETWEENTIME_ELEMENT) ??
			                         DEFAULT_BETWEEN_TIME;
		}
	}
}
