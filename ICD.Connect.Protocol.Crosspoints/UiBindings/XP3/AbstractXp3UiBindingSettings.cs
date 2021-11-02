using ICD.Common.Utils.Xml;
using ICD.Connect.Themes.UiBindings;

namespace ICD.Connect.Protocol.Crosspoints.UiBindings.XP3
{
	public abstract class AbstractXp3UiBindingSettings1Originator : AbstractUiBindingSettings1Originator, IXp3UiBindingSettings1Originator
	{
		private const string ELEMENT_XP3_SYSTEM_ID = "Xp3SystemId";

		public int? Xp3SystemId { get; set; }

		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(ELEMENT_XP3_SYSTEM_ID, IcdXmlConvert.ToString(Xp3SystemId));
		}

		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Xp3SystemId = XmlUtils.TryReadChildElementContentAsInt(xml, ELEMENT_XP3_SYSTEM_ID);
		}
	}

	public abstract class AbstractXp3UiBindingSettings2Originators : AbstractUiBindingSettings2Originators, IXp3UiBindingSettings2Originators
	{
		private const string ELEMENT_XP3_SYSTEM_ID = "Xp3SystemId";
	
		public int? Xp3SystemId { get; set; }

		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(ELEMENT_XP3_SYSTEM_ID, IcdXmlConvert.ToString(Xp3SystemId));
		}

		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Xp3SystemId = XmlUtils.TryReadChildElementContentAsInt(xml, ELEMENT_XP3_SYSTEM_ID);
		}
	}

	public abstract class AbstractXp3UiBindingSettings3Originators : AbstractUiBindingSettings3Originators, IXp3UiBindingSettings3Originators
	{
		private const string ELEMENT_XP3_SYSTEM_ID = "Xp3SystemId";

		public int? Xp3SystemId { get; set; }

		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(ELEMENT_XP3_SYSTEM_ID, IcdXmlConvert.ToString(Xp3SystemId));
		}

		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Xp3SystemId = XmlUtils.TryReadChildElementContentAsInt(xml, ELEMENT_XP3_SYSTEM_ID);
		}
	}

	public abstract class AbstractXp3UiBindingSettings4Originators : AbstractUiBindingSettings4Originators, IXp3UiBindingSettings4Originators
	{
		private const string ELEMENT_XP3_SYSTEM_ID = "Xp3SystemId";

		public int? Xp3SystemId { get; set; }

		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(ELEMENT_XP3_SYSTEM_ID, IcdXmlConvert.ToString(Xp3SystemId));
		}

		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Xp3SystemId = XmlUtils.TryReadChildElementContentAsInt(xml, ELEMENT_XP3_SYSTEM_ID);
		}
	}
}
