using ICD.Common.Utils.Xml;

namespace ICD.Connect.Protocol.Crosspoints.UiBindings.XP3.Equipment
{
	public abstract class AbstractXp3EquipmentUiBindingSettings1Originator : AbstractXp3UiBindingSettings1Originator, IXp3EquipmentUiBindingSettings1Originator
	{
		private const string ELEMENT_XP3_EQUIPMENT_ID = "Xp3EquipmentId";

		public int? Xp3EquipmentId { get; set; }

		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(ELEMENT_XP3_EQUIPMENT_ID, IcdXmlConvert.ToString(Xp3EquipmentId));
		}

		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Xp3EquipmentId = XmlUtils.TryReadChildElementContentAsInt(xml, ELEMENT_XP3_EQUIPMENT_ID);
		}
	}

	public abstract class AbstractXp3EquipmentUiBindingSettings2Originators : AbstractXp3UiBindingSettings2Originators, IXp3EquipmentUiBindingSettings2Originators
	{
		private const string ELEMENT_XP3_EQUIPMENT_ID = "Xp3EquipmentId";

		public int? Xp3EquipmentId { get; set; }

		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(ELEMENT_XP3_EQUIPMENT_ID, IcdXmlConvert.ToString(Xp3EquipmentId));
		}

		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Xp3EquipmentId = XmlUtils.TryReadChildElementContentAsInt(xml, ELEMENT_XP3_EQUIPMENT_ID);
		}
	}

	public abstract class AbstractXp3EquipmentUiBindingSettings3Originators : AbstractXp3UiBindingSettings3Originators, IXp3EquipmentUiBindingSettings3Originators
	{
		private const string ELEMENT_XP3_EQUIPMENT_ID = "Xp3EquipmentId";

		public int? Xp3EquipmentId { get; set; }

		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(ELEMENT_XP3_EQUIPMENT_ID, IcdXmlConvert.ToString(Xp3EquipmentId));
		}

		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Xp3EquipmentId = XmlUtils.TryReadChildElementContentAsInt(xml, ELEMENT_XP3_EQUIPMENT_ID);
		}
	}

	public abstract class AbstractXp3EquipmentUiBindingSettings4Originators : AbstractXp3UiBindingSettings4Originators, IXp3EquipmentUiBindingSettings4Originators
	{
		private const string ELEMENT_XP3_EQUIPMENT_ID = "Xp3EquipmentId";

		public int? Xp3EquipmentId { get; set; }

		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(ELEMENT_XP3_EQUIPMENT_ID, IcdXmlConvert.ToString(Xp3EquipmentId));
		}

		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Xp3EquipmentId = XmlUtils.TryReadChildElementContentAsInt(xml, ELEMENT_XP3_EQUIPMENT_ID);
		}
	}
}