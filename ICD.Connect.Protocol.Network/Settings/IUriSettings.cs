namespace ICD.Connect.Protocol.Network.Settings
{
	public interface IUriSettings : INetworkSettings
	{
		/// <summary>
		/// Gets/sets the configurable URI scheme.
		/// </summary>
		string UriScheme { get; set; }

		/// <summary>
		/// Gets/sets the configurable URI path.
		/// </summary>
		string UriPath { get; set; }

		/// <summary>
		/// Gets/sets the configurable URI query.
		/// </summary>
		string UriQuery { get; set; }

		/// <summary>
		/// Gets/sets the configurable URI fragment.
		/// </summary>
		string UriFragment { get; set; }
	}
}
