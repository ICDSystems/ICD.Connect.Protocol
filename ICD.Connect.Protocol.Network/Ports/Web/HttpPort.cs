using System;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Connect.Protocol.Network.Settings;

namespace ICD.Connect.Protocol.Network.Ports.Web
{
	/// <summary>
	/// Allows for communication with a HTTP service.
	/// </summary>
	public sealed partial class HttpPort : AbstractWebPort<HttpPortSettings>
	{
		private const string SOAP_ACCEPT = "application/xml";
		private const string SOAP_ACTION_HEADER = "SOAPAction";

		private readonly UriProperties m_UriProperties = new UriProperties();
		private readonly WebProxyProperties m_WebProxyProperties = new WebProxyProperties();

		private bool m_LastRequestSucceeded;

		#region Properties

		/// <summary>
		/// Gets the configured URI properties.
		/// </summary>
		public override IUriProperties UriProperties { get { return m_UriProperties; } }

		/// <summary>
		/// Gets the proxy configuration for the web port.
		/// </summary>
		public override IWebProxyProperties WebProxyProperties { get { return m_WebProxyProperties; } }

		/// <summary>
		/// The base URI for requests.
		/// </summary>
		[CanBeNull]
		public override Uri Uri { get; set; }

		/// <summary>
		/// Gets/sets the proxy URI.
		/// </summary>
		[CanBeNull]
		public override Uri ProxyUri { get; set; }

		/// <summary>
		/// Gets/sets the proxy authentication method.
		/// </summary>
		public override eProxyAuthenticationMethod ProxyAuthenticationMethod { get; set; }

		#endregion

		#region Private Methods

		/// <summary>
		/// Builds a request url from the given relative or absolute uri.
		/// e.g.
		///		"Test/Path"
		/// May result in
		///		https://10.3.14.15/Test/Path
		/// </summary>
		/// <param name="relativeOrAbsolute"></param>
		/// <returns></returns>
		private Uri GetRequestUrl(string relativeOrAbsolute)
		{
			IcdUriBuilder builder =
				Uri == null
					? new IcdUriBuilder()
					: new IcdUriBuilder(Uri);

			// When no relative or absolute path is specified we return the URI configured on the port.
			if (relativeOrAbsolute == null)
				return builder.Uri;

			// Is the path absolute?
			Uri uri;
			if (Uri.TryCreate(relativeOrAbsolute, UriKind.Absolute, out uri) &&
				!uri.IsFile) // Hack - relative paths are parsed as absolute file paths in 3.5
				builder = new IcdUriBuilder(uri);
			else
				builder.AppendPath(relativeOrAbsolute);

#if SIMPLSHARP
			// Crestron tries to strip out encoded spaces (and possibly other encodings?) from the path
			// so we doubly-escape to prevent this from happening.
			builder.Path = string.IsNullOrEmpty(builder.Path)
				? builder.Path
				: builder.Path.Replace("%", "%25");
#endif

			return builder.Uri;
		}

		/// <summary>
		/// Updates the IsOnline status and raises events.
		/// </summary>
		/// <param name="succeeded"></param>
		private void SetLastRequestSucceeded(bool succeeded)
		{
			m_LastRequestSucceeded = succeeded;
			UpdateCachedOnlineStatus();
		}

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return m_LastRequestSucceeded;
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			// Reset the online state
			SetLastRequestSucceeded(true);
		}

		#endregion
	}
}
