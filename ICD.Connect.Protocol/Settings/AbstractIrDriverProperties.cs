using System;
using ICD.Common.Utils.Xml;

namespace ICD.Connect.Protocol.Settings
{
	public abstract class AbstractIrDriverProperties : IIrDriverProperties
	{
		public const ushort DEFAULT_PULSE_TIME = 100;
		public const ushort DEFAULT_BETWEEN_TIME = 750;

		private const string DRIVER_ELEMENT = "Driver";
		private const string PULSETIME_ELEMENT = "PulseTime";
		private const string BETWEENTIME_ELEMENT = "BetweenTime";

		#region Properties

		/// <summary>
		/// Gets/sets the configurable path to the IR driver.
		/// </summary>
		public string IrDriverPath { get; set; }

		/// <summary>
		/// Gets/sets the configurable pulse time for the IR driver.
		/// </summary>
		public ushort IrPulseTime { get; set; }

		/// <summary>
		/// Gets/sets the configurable between time for the IR driver.
		/// </summary>
		public ushort IrBetweenTime { get; set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractIrDriverProperties()
		{
			IrPulseTime = DEFAULT_PULSE_TIME;
			IrBetweenTime = DEFAULT_BETWEEN_TIME;
		}

		/// <summary>
		/// Writes the IR configuration to xml.
		/// </summary>
		/// <param name="writer"></param>
		public void WriteElements(IcdXmlTextWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			writer.WriteElementString(DRIVER_ELEMENT, IrDriverPath);
			writer.WriteElementString(PULSETIME_ELEMENT, IcdXmlConvert.ToString(IrPulseTime));
			writer.WriteElementString(BETWEENTIME_ELEMENT, IcdXmlConvert.ToString(IrBetweenTime));
		}

		/// <summary>
		/// Reads the IR configuration from xml.
		/// </summary>
		/// <param name="xml"></param>
		public void ParseXml(string xml)
		{
			IrDriverPath = XmlUtils.TryReadChildElementContentAsString(xml, DRIVER_ELEMENT);
			IrPulseTime = XmlUtils.TryReadChildElementContentAsUShort(xml, PULSETIME_ELEMENT) ?? DEFAULT_PULSE_TIME;
			IrBetweenTime = XmlUtils.TryReadChildElementContentAsUShort(xml, BETWEENTIME_ELEMENT) ??
			                DEFAULT_BETWEEN_TIME;
		}
	}
}
