using System;
using System.Collections.Generic;
using System.Text;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.Network.Ports.Web
{
	public abstract class AbstractWebPort<TSettings> : AbstractPort<TSettings>, IWebPort
		where TSettings : IWebPortSettings, new()
	{
		/// <summary>
		/// Gets the URI configuration for the web port.
		/// </summary>
		public abstract UriProperties UriProperties { get; }

		/// <summary>
		/// Content type for the server to respond with. See HttpClient.Accept.
		/// </summary>
		public abstract string Accept { get; set; }

		/// <summary>
		/// Returns true if currently waiting for a response from the server.
		/// </summary>
		public abstract bool Busy { get; }

		/// <summary>
		/// Sends a GET request to the server.
		/// </summary>
		/// <param name="localUrl"></param>
		/// <param name="headers"></param>
		/// <param name="response"></param>
		public abstract bool Get(string localUrl, IDictionary<string, List<string>> headers, out string response);

		/// <summary>
		/// Sends a GET request to the server.
		/// </summary>
		/// <param name="localUrl"></param>
		/// <param name="response"></param>
		public abstract bool Get(string localUrl, out string response);

		/// <summary>
		/// Sends a POST request to the server.
		/// </summary>
		/// <param name="localUrl"></param>
		/// <param name="data"></param>
		/// <param name="response"></param>
		/// <returns></returns>
		public abstract bool Post(string localUrl, byte[] data, out string response);

		/// <summary>
		/// Sends a POST request to the server.
		/// </summary>
		/// <param name="localUrl"></param>
		/// <param name="data"></param>
		/// <param name="encoding"></param>
		/// <param name="response"></param>
		/// <returns></returns>
		public abstract bool Post(string localUrl, string data, Encoding encoding, out string response);

		/// <summary>
		/// Sends a SOAP request to the server.
		/// </summary>
		/// <param name="action"></param>
		/// <param name="content"></param>
		/// <param name="response"></param>
		/// <returns></returns>
		public abstract bool DispatchSoap(string action, string content, out string response);

		/// <summary>
		/// Applies the given device configuration properties to the port.
		/// </summary>
		/// <param name="properties"></param>
		public void ApplyDeviceConfiguration(IUriProperties properties)
		{
			if (properties == null)
				throw new ArgumentNullException("properties");

			// Port supercedes device configuration
			IUriProperties config = UriProperties.Superimpose(properties);

			ApplyConfiguration(config);
		}

		/// <summary>
		/// Applies the given configuration properties to the port.
		/// </summary>
		/// <param name="properties"></param>
		public void ApplyConfiguration(IUriProperties properties)
		{
			if (properties == null)
				throw new ArgumentNullException("properties");

			UriProperties.Copy(properties);
		}
	}
}
