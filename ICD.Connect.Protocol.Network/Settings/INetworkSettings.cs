using ICD.Connect.Settings;

namespace ICD.Connect.Protocol.Network.Settings
{
	public interface INetworkSettings : IUsernamePasswordSettings
	{
		/// <summary>
		/// Gets/sets the configurable network address.
		/// </summary>
		string NetworkAddress { get; set; }

		/// <summary>
		/// Gets/sets the configurable network port.
		/// </summary>
		ushort NetworkPort { get; set; }
	}
}
