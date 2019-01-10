using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.Network.Ports.Web
{
	/// <summary>
	/// Settings for a HttpPort.
	/// </summary>
	[KrangSettings("HTTP", typeof(HttpPort))]
	public sealed class HttpPortSettings : AbstractWebPortSettings
	{
	}
}
