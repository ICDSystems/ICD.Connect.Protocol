using System.Text;
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

	/// <summary>
	/// Extension methods for IWebPort.
	/// </summary>
	public static class WebPortExtensions
	{
		/// <summary>
		/// Sends a POST request to the server. Assumes data is ASCII.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="localUrl"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		[PublicAPI]
		public static string Post(this IWebPort extends, string localUrl, string data)
		{
			return extends.Post(localUrl, data, new ASCIIEncoding());
		}

		/// <summary>
		/// Sends a POST request to the server using the given encoding for data.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="localUrl"></param>
		/// <param name="data"></param>
		/// <param name="encoding"></param>
		/// <returns></returns>
		[PublicAPI]
		public static string Post(this IWebPort extends, string localUrl, string data, Encoding encoding)
		{
			byte[] bytes = encoding.GetBytes(data);
			return extends.Post(localUrl, bytes);
		}
	}
}
