namespace ICD.Connect.Protocol.Network.WebPorts.Http
{
	/// <summary>
	/// Allows for communication with a HTTP device.
	/// </summary>
	public sealed partial class HttpPort : AbstractWebPort<HttpPortSettings>
	{
		/// <summary>
		/// The request protocol, i.e. http or https.
		/// </summary>
		protected override string Protocol { get { return "http"; } }
	}
}
