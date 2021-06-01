using System;
using ICD.Common.Utils.Xml;
using ICD.Connect.Protocol.Network.Ports.Web;

namespace ICD.Connect.Protocol.Network.Settings
{
	public abstract class AbstractWebProxyProperties : IWebProxyProperties
	{
		private const string ELEMENT = "Proxy";

		private const string PROXY_USERNAME_ELEMENT = "Username";
		private const string PROXY_PASSWORD_ELEMENT = "Password";
		private const string PROXY_HOST_ELEMENT = "Host";
		private const string PROXY_PORT_ELEMENT = "Port";
		private const string PROXY_SCHEME_ELEMENT = "Scheme";
		private const string PROXY_METHOD_ELEMENT = "Method";

		#region Properties

		/// <summary>
		/// Gets/sets the configurable proxy username.
		/// </summary>
		public string ProxyUsername { get; set; }

		/// <summary>
		/// Gets/sets the configurable proxy password.
		/// </summary>
		public string ProxyPassword { get; set; }

		/// <summary>
		/// Gets/sets the configurable proxy host.
		/// </summary>
		public string ProxyHost { get; set; }

		/// <summary>
		/// Gets/sets the configurable proxy port.
		/// </summary>
		public ushort? ProxyPort { get; set; }

		/// <summary>
		/// Gets/sets the configurable proxy scheme.
		/// </summary>
		public string ProxyScheme { get; set; }

		/// <summary>
		/// Gets/sets the configurable proxy authentication method.
		/// </summary>
		public eProxyAuthenticationMethod? ProxyAuthenticationMethod { get; set; }

		#endregion

		#region Methods

		/// <summary>
		/// Clears the configured properties.
		/// </summary>
		public void ClearProxyProperties()
		{
			ProxyUsername = null;
			ProxyPassword = null;
			ProxyHost = null;
			ProxyPort = null;
			ProxyScheme = null;
			ProxyAuthenticationMethod = null;
		}

		/// <summary>
		/// Writes the username and password configuration to xml.
		/// </summary>
		/// <param name="writer"></param>
		public void WriteElements(IcdXmlTextWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			writer.WriteStartElement(ELEMENT);
			{
				writer.WriteElementString(PROXY_USERNAME_ELEMENT, ProxyUsername);
				writer.WriteElementString(PROXY_PASSWORD_ELEMENT, ProxyPassword);
				writer.WriteElementString(PROXY_HOST_ELEMENT, ProxyHost);
				writer.WriteElementString(PROXY_PORT_ELEMENT, IcdXmlConvert.ToString(ProxyPort));
				writer.WriteElementString(PROXY_SCHEME_ELEMENT, IcdXmlConvert.ToString(ProxyScheme));
				writer.WriteElementString(PROXY_METHOD_ELEMENT, IcdXmlConvert.ToString(ProxyAuthenticationMethod));
			}
			writer.WriteEndElement();
		}

		/// <summary>
		/// Reads the username and password configuration from xml.
		/// </summary>
		/// <param name="xml"></param>
		public void ParseXml(string xml)
		{
			ClearProxyProperties();

			string proxy;
			if (!XmlUtils.TryGetChildElementAsString(xml, ELEMENT, out proxy))
				return;

			string proxyUsername = XmlUtils.TryReadChildElementContentAsString(proxy, PROXY_USERNAME_ELEMENT);
			string proxyPassword = XmlUtils.TryReadChildElementContentAsString(proxy, PROXY_PASSWORD_ELEMENT);
			string proxyHost = XmlUtils.TryReadChildElementContentAsString(proxy, PROXY_HOST_ELEMENT);
			string proxyScheme = XmlUtils.TryReadChildElementContentAsString(proxy, PROXY_SCHEME_ELEMENT);

			// If strings are empty, set the value as null so overrides will work properly
			ProxyUsername = string.IsNullOrEmpty(proxyUsername) ? null : proxyUsername.Trim();
			ProxyPassword = string.IsNullOrEmpty(proxyPassword) ? null : proxyPassword.Trim();
			ProxyHost = string.IsNullOrEmpty(proxyHost) ? null : proxyHost.Trim();
			ProxyPort = XmlUtils.TryReadChildElementContentAsUShort(proxy, PROXY_PORT_ELEMENT);
			ProxyScheme = string.IsNullOrEmpty(proxyScheme) ? null : proxyScheme.Trim();
			ProxyAuthenticationMethod = XmlUtils.TryReadChildElementContentAsEnum<eProxyAuthenticationMethod>(proxy, PROXY_METHOD_ELEMENT, true);
		}

		#endregion
	}
}
