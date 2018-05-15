using ICD.Connect.Settings;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Protocol.Settings
{
	public interface IIrDriverSettings : ISettings, IIrDriverProperties
	{
		/// <summary>
		/// Gets the configurable IR driver properties.
		/// </summary>
		[HiddenSettingsProperty]
		IIrDriverProperties IrDriverProperties { get; }
	}
}
