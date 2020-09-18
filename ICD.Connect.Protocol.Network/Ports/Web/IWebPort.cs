using System;
using ICD.Common.Properties;
using ICD.Connect.Protocol.Network.Settings;
﻿using System.Collections.Generic;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.Network.Ports.Web
{
	/// <summary>
	/// Interface for web communication (i.e http, https)
	/// </summary>
	public interface IWebPort : IPort
	{
		#region Properties

		/// <summary>
		/// Gets the URI configuration for the web port.
		/// </summary>
		[PublicAPI]
		IUriProperties UriProperties { get; }

		/// <summary>
		/// Gets the proxy configuration for the web port.
		/// </summary>
		[PublicAPI]
		IWebProxyProperties WebProxyProperties { get; }

		/// <summary>
		/// Gets/sets the base URI for requests.
		/// </summary>
		[CanBeNull]
		Uri Uri { get; set; }

		/// <summary>
		/// Gets/sets the proxy URI.
		/// </summary>
		[CanBeNull]
		Uri ProxyUri { get; set; }

		/// <summary>
		/// Gets/sets the proxy authentication method.
		/// </summary>
		[PublicAPI]
		eProxyAuthenticationMethod ProxyAuthenticationMethod { get; set; }

		/// <summary>
		/// Gets/sets the content type for the server to respond with.
		/// </summary>
		[PublicAPI]
		string Accept { get; set; }

		/// <summary>
		/// Returns true if currently waiting for a response from the server.
		/// </summary>
		[PublicAPI]
		bool Busy { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Sends a GET request to the server.
		/// </summary>
		/// <param name="relativeOrAbsoluteUri"></param>
		[PublicAPI]
		WebPortResponse Get(string relativeOrAbsoluteUri);

		/// <summary>
		/// Sends a GET request to the server.
		/// </summary>
		/// <param name="relativeOrAbsoluteUri"></param>
		/// <param name="headers"></param>
		[PublicAPI]
		WebPortResponse Get(string relativeOrAbsoluteUri, IDictionary<string, List<string>> headers);

		/// <summary>
		/// Sends a POST request to the server.
		/// </summary>
		/// <param name="relativeOrAbsoluteUri"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		[PublicAPI]
		WebPortResponse Post(string relativeOrAbsoluteUri, byte[] data);

		/// <summary>
		/// Sends a POST request to the server.
		/// </summary>
		/// <param name="relativeOrAbsoluteUri"></param>
		/// <param name="headers"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		[PublicAPI]
		WebPortResponse Post(string relativeOrAbsoluteUri, Dictionary<string, List<string>> headers, byte[] data);

		/// <summary>
		/// Sends a PATCH request to the server.
		/// </summary>
		/// <param name="relativeOrAbsoluteUri"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		[PublicAPI]
		WebPortResponse Patch(string relativeOrAbsoluteUri, byte[] data);

		/// <summary>
		/// Sends a PATCH request to the server.
		/// </summary>
		/// <param name="relativeOrAbsoluteUri"></param>
		/// <param name="headers"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		[PublicAPI]
		WebPortResponse Patch(string relativeOrAbsoluteUri, Dictionary<string, List<string>> headers, byte[] data);

		/// <summary>
		/// Sends a PUT request to the server.
		/// </summary>
		/// <param name="relativeOrAbsoluteUri"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		[PublicAPI]
		WebPortResponse Put(string relativeOrAbsoluteUri, byte[] data);

		/// <summary>
		/// Sends a PUT request to the server.
		/// </summary>
		/// <param name="relativeOrAbsoluteUri"></param>
		/// <param name="headers"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		[PublicAPI]
		WebPortResponse Put(string relativeOrAbsoluteUri, Dictionary<string, List<string>> headers, byte[] data);

		/// <summary>
		/// Sends a SOAP request to the server.
		/// </summary>
		/// <param name="action"></param>
		/// <param name="content"></param>
		/// <returns></returns>
		[PublicAPI]
		WebPortResponse DispatchSoap(string action, string content);

		/// <summary>
		/// Applies the given device configuration properties to the port.
		/// </summary>
		/// <param name="properties"></param>
		void ApplyDeviceConfiguration(IUriProperties properties);

		/// <summary>
		/// Applies the given device configuration properties to the port.
		/// </summary>
		/// <param name="properties"></param>
		void ApplyDeviceConfiguration(IWebProxyProperties properties);

		#endregion
	}
}
