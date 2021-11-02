using ICD.Connect.Themes.UiBindings;

namespace ICD.Connect.Protocol.Crosspoints.UiBindings.XP3
{
	public interface IXp3UiBindingSettings : IUiBindingSettings
	{

		int? Xp3SystemId { get; set; }

	}

	public interface IXp3UiBindingSettings1Originator : IXp3UiBindingSettings, IUiBindingSettings1Originator
	{
	}

	public interface IXp3UiBindingSettings2Originators : IXp3UiBindingSettings, IUiBindingSettings2Originators
	{
	}

	public interface IXp3UiBindingSettings3Originators : IXp3UiBindingSettings, IUiBindingSettings3Originators
	{
	}

	public interface IXp3UiBindingSettings4Originators : IXp3UiBindingSettings, IUiBindingSettings4Originators
	{
	}
}
