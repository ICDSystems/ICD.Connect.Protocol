using ICD.Common.Utils.Xml;

namespace ICD.Connect.Protocol.Crosspoints.UiBindings.XP3.Control
{
	public abstract class AbstractXp3ControlUiBindingSettings1Originator : AbstractXp3UiBindingSettings1Originator, IXp3ControlUiBindingSettings1Originator
	{
		private const string ELEMENT_XP3_CONTROL_ID = "Xp3ControlId";

		public int? Xp3ControlId { get; set; }

		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(ELEMENT_XP3_CONTROL_ID, IcdXmlConvert.ToString(Xp3ControlId));
		}

		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Xp3ControlId = XmlUtils.TryReadChildElementContentAsInt(xml, ELEMENT_XP3_CONTROL_ID);
		}
	}

	public abstract class AbstractXp3ControlUiBindingSettings2Originators : AbstractXp3UiBindingSettings2Originators, IXp3ControlUiBindingSettings2Originators
	{
		private const string ELEMENT_XP3_CONTROL_ID = "Xp3ControlId";

		public int? Xp3ControlId { get; set; }

		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(ELEMENT_XP3_CONTROL_ID, IcdXmlConvert.ToString(Xp3ControlId));
		}

		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Xp3ControlId = XmlUtils.TryReadChildElementContentAsInt(xml, ELEMENT_XP3_CONTROL_ID);
		}
	}

	public abstract class AbstractXp3ControlUiBindingSettings3Originators : AbstractXp3UiBindingSettings3Originators, IXp3ControlUiBindingSettings3Originators
	{
		private const string ELEMENT_XP3_CONTROL_ID = "Xp3ControlId";

		public int? Xp3ControlId { get; set; }

		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(ELEMENT_XP3_CONTROL_ID, IcdXmlConvert.ToString(Xp3ControlId));
		}

		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Xp3ControlId = XmlUtils.TryReadChildElementContentAsInt(xml, ELEMENT_XP3_CONTROL_ID);
		}
	}

	public abstract class AbstractXp3ControlUiBindingSettings4Originators : AbstractXp3UiBindingSettings4Originators, IXp3ControlUiBindingSettings4Originators
	{
		private const string ELEMENT_XP3_CONTROL_ID = "Xp3ControlId";

		public int? Xp3ControlId { get; set; }

		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(ELEMENT_XP3_CONTROL_ID, IcdXmlConvert.ToString(Xp3ControlId));
		}

		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Xp3ControlId = XmlUtils.TryReadChildElementContentAsInt(xml, ELEMENT_XP3_CONTROL_ID);
		}
	}
}