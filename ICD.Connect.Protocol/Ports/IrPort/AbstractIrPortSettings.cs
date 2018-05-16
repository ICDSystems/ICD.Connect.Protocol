using ICD.Common.Utils.Xml;
using ICD.Connect.Protocol.Settings;

namespace ICD.Connect.Protocol.Ports.IrPort
{
	public abstract class AbstractIrPortSettings : AbstractPortSettings, IIrPortSettings
	{
		private readonly IrDriverProperties m_IrDriverProperties;

		#region Properties

		/// <summary>
		/// Gets/sets the configurable path to the IR driver.
		/// </summary>
		public string IrDriverPath { get; set; }

		/// <summary>
		/// Gets/sets the configurable pulse time for the IR driver.
		/// </summary>
		public ushort IrPulseTime
		{
			get { return m_IrDriverProperties.IrPulseTime; }
			set { m_IrDriverProperties.IrPulseTime = value; }
		}

		/// <summary>
		/// Gets/sets the configurable between time for the IR driver.
		/// </summary>
		public ushort IrBetweenTime
		{
			get { return m_IrDriverProperties.IrBetweenTime; }
			set { m_IrDriverProperties.IrBetweenTime = value; }
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractIrPortSettings()
		{
			m_IrDriverProperties = new IrDriverProperties();
		}

		#region Method

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			m_IrDriverProperties.WriteElements(writer);
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			m_IrDriverProperties.ParseXml(xml);
		}

		#endregion
	}
}
