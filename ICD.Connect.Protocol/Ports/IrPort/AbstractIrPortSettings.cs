using ICD.Common.Utils.Xml;
using ICD.Connect.Protocol.Settings;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Protocol.Ports.IrPort
{
	public abstract class AbstractIrPortSettings : AbstractPortSettings, IIrPortSettings
	{
		/// <summary>
		/// Gets/sets the configurable path to the IR driver.
		/// </summary>
		[PathSettingsProperty("IRDrivers", ".ir")]
		public string IrDriverPath { get; set; }

		/// <summary>
		/// Gets/sets the configurable pulse time for the IR driver.
		/// </summary>
		public ushort IrPulseTime { get; set; }

		/// <summary>
		/// Gets/sets the configurable between time for the IR driver.
		/// </summary>
		public ushort IrBetweenTime { get; set; }

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractIrPortSettings()
		{
			IrPulseTime = IrDriverSettingsParsing.DEFAULT_PULSE_TIME;
			IrBetweenTime = IrDriverSettingsParsing.DEFAULT_BETWEEN_TIME;
		}

		#region Method

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			IrDriverSettingsParsing.WriteElements(writer, this);
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			IrDriverSettingsParsing.ParseXml(xml, this);
		}

		#endregion
	}
}
