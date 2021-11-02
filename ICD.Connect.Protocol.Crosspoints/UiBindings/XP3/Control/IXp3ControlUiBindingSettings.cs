namespace ICD.Connect.Protocol.Crosspoints.UiBindings.XP3.Control
{
	public interface IXp3ControlUiBindingSettings : IXp3UiBindingSettings
	{
		int? Xp3ControlId { get; set; }
	}

	public interface IXp3ControlUiBindingSettings1Originator : IXp3ControlUiBindingSettings, IXp3UiBindingSettings1Originator
	{
	}

	public interface IXp3ControlUiBindingSettings2Originators : IXp3ControlUiBindingSettings, IXp3UiBindingSettings2Originators
	{
	}

	public interface IXp3ControlUiBindingSettings3Originators : IXp3ControlUiBindingSettings, IXp3UiBindingSettings3Originators
	{
	}

	public interface IXp3ControlUiBindingSettings4Originators : IXp3ControlUiBindingSettings, IXp3UiBindingSettings4Originators
	{
	}

}