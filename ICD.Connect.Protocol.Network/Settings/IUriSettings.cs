using ICD.Connect.Settings;

namespace ICD.Connect.Protocol.Network.Settings
{
	public interface IUriSettings : ISettings
	{
		/// <summary>
		/// Gets/sets the configurable URI scheme.
		/// </summary>
		string UriScheme { get; set; }

		/// <summary>
		/// Gets/sets the configurable URI user info.
		/// </summary>
		string UriUserInfo { get; set; }

		/// <summary>
		/// Gets/sets the configurable URI host.
		/// </summary>
		string UriHost { get; set; }

		/// <summary>
		/// Gets/sets the configurable URI port.
		/// </summary>
		ushort UriPort { get; set; }

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
