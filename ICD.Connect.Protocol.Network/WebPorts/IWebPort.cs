using System.Collections.Generic;
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
		/// <param name="headers"></param>
		/// <param name="response"></param>
		[PublicAPI]
		bool Get(string localUrl, IDictionary<string, List<string>> headers, out string response);

		/// <summary>
		/// Sends a GET request to the server.
		/// </summary>
		/// <param name="localUrl"></param>
		/// <param name="response"></param>
		[PublicAPI]
		bool Get(string localUrl, out string response);

		/// <summary>
		/// Sends a POST request to the server.
		/// </summary>
		/// <param name="localUrl"></param>
		/// <param name="data"></param>
		/// <param name="response"></param>
		/// <returns></returns>
		[PublicAPI]
		bool Post(string localUrl, byte[] data, out string response);

		/// <summary>
		/// Sends a SOAP request to the server.
		/// </summary>
		/// <param name="action"></param>
		/// <param name="content"></param>
		/// <param name="response"></param>
		/// <returns></returns>
		[PublicAPI]
		bool DispatchSoap(string action, string content, out string response);
	}
}
