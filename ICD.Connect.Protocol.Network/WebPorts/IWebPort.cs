using ICD.Common.Properties;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.Network.WebPorts
{
	/// <summary>
	/// Interface for web communication (i.e http, https)
	/// </summary>
	public interface IWebPort : IPort
	{
		/// <summary>
		/// The server address.
		/// </summary>
		[PublicAPI]
		string Address { get; set; }

		/// <summary>
		/// Content type for the server to respond with. See HttpClient.Accept.
		/// </summary>
		[PublicAPI]
		string Accept { get; set; }

		/// <summary>
		/// Username for the server.
		/// </summary>
		[PublicAPI]
		string Username { get; set; }

		/// <summary>
		/// Password for the server.
		/// </summary>
		[PublicAPI]
		string Password { get; set; }

		/// <summary>
		/// Returns true if currently waiting for a response from the server.
		/// </summary>
		[PublicAPI]
		bool Busy { get; }

		/// <summary>
		/// Sends a GET request to the server.
		/// </summary>
		/// <param name="localUrl"></param>
		[PublicAPI]
		string Get(string localUrl);

		/// <summary>
		/// Sends a POST request to the server.
		/// </summary>
		/// <param name="localUrl"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		[PublicAPI]
		string Post(string localUrl, byte[] data);

		/// <summary>
		/// Sends a SOAP request to the server.
		/// </summary>
		/// <param name="action"></param>
		/// <param name="content"></param>
		/// <returns></returns>
		[PublicAPI]
		string DispatchSoap(string action, string content);
	}
}
