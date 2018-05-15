using ICD.Connect.Settings;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Protocol.Network.Settings
{
	public interface IUriSettings : ISettings, IUriProperties
	{
		/// <summary>
		/// Gets the configurable URI properties.
		/// </summary>
		[HiddenSettingsProperty]
		IUriProperties UriProperties { get; }
	}
}
