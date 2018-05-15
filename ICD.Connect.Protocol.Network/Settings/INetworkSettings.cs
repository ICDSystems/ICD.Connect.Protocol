using ICD.Connect.Settings;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Protocol.Network.Settings
{
	public interface INetworkSettings : ISettings, INetworkProperties
	{
		/// <summary>
		/// Gets the configurable network properties.
		/// </summary>
		[HiddenSettingsProperty]
		INetworkProperties NetworkProperties { get; }
	}
}
