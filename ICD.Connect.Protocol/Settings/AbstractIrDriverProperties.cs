using System;
using ICD.Common.Utils.Xml;

namespace ICD.Connect.Protocol.Settings
{
	public abstract class AbstractIrDriverProperties : IIrDriverProperties
	{
		private const string ELEMENT = "IR";

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
		public ushort? IrPulseTime { get; set; }

		/// <summary>
		/// Gets/sets the configurable between time for the IR driver.
		/// </summary>
		public ushort? IrBetweenTime { get; set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractIrDriverProperties()
		{
			ClearIrProperties();
		}

		#region Properties

		/// <summary>
		/// Clears the configured properties.
		/// </summary>
		public void ClearIrProperties()
		{
			IrDriverPath = null;
			IrPulseTime = null;
			IrBetweenTime = null;
		}

		/// <summary>
		/// Writes the IR configuration to xml.
		/// </summary>
		/// <param name="writer"></param>
		public void WriteElements(IcdXmlTextWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			writer.WriteStartElement(ELEMENT);
			{
				writer.WriteElementString(DRIVER_ELEMENT, IrDriverPath);
				writer.WriteElementString(PULSETIME_ELEMENT, IcdXmlConvert.ToString(IrPulseTime));
				writer.WriteElementString(BETWEENTIME_ELEMENT, IcdXmlConvert.ToString(IrBetweenTime));
			}
			writer.WriteEndElement();
		}

		/// <summary>
		/// Reads the IR configuration from xml.
		/// </summary>
		/// <param name="xml"></param>
		public void ParseXml(string xml)
		{
			ClearIrProperties();

			string ir;
			if (!XmlUtils.TryGetChildElementAsString(xml, ELEMENT, out ir))
				return;

			IrDriverPath = XmlUtils.TryReadChildElementContentAsString(ir, DRIVER_ELEMENT);
			IrPulseTime = XmlUtils.TryReadChildElementContentAsUShort(ir, PULSETIME_ELEMENT);
			IrBetweenTime = XmlUtils.TryReadChildElementContentAsUShort(ir, BETWEENTIME_ELEMENT);
		}

		#endregion
	}
}
