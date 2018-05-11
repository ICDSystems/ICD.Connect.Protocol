using ICD.Connect.Settings;

namespace ICD.Connect.Protocol.Network.Settings
{
	public interface INetworkSettings : ISettings
	{
		/// <summary>
		/// Gets/sets the configurable network address.
		/// </summary>
		string NetworkAddress { get; set; }

		/// <summary>
		/// Gets/sets the configurable network port.
		/// </summary>
		string NetworkPort { get; set; }
	}
}
