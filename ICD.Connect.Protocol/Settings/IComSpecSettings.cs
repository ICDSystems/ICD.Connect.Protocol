using ICD.Connect.Settings;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Protocol.Settings
{
	public interface IComSpecSettings : ISettings, IComSpecProperties
	{
		/// <summary>
		/// Gets the configurable Com Spec properties.
		/// </summary>
		[HiddenSettingsProperty]
		IComSpecProperties ComSpecProperties { get; }
	}
}
