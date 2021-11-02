namespace ICD.Connect.Protocol.Crosspoints.UiBindings.XP3.Equipment
{
	public interface IXp3EquipmentUiBindingSettings : IXp3UiBindingSettings
	{
		int? Xp3EquipmentId { get; set; }
	}

	public interface IXp3EquipmentUiBindingSettings1Originator : IXp3EquipmentUiBindingSettings, IXp3UiBindingSettings1Originator
	{
	}

	public interface IXp3EquipmentUiBindingSettings2Originators : IXp3EquipmentUiBindingSettings, IXp3UiBindingSettings2Originators
	{
	}

	public interface IXp3EquipmentUiBindingSettings3Originators : IXp3EquipmentUiBindingSettings, IXp3UiBindingSettings3Originators
	{
	}

	public interface IXp3EquipmentUiBindingSettings4Originators : IXp3EquipmentUiBindingSettings, IXp3UiBindingSettings4Originators
	{
	}
}