using System;
using ICD.Common.Utils.Xml;
using ICD.Connect.Protocol.Network.Ports.Web;

namespace ICD.Connect.Protocol.Network.Settings
{
	public abstract class AbstractProxyProperties : IProxyProperties
	{
		private const string ELEMENT = "Proxy";

		private const string PROXY_USERNAME_ELEMENT = "Username";
		private const string PROXY_PASSWORD_ELEMENT = "Password";
		private const string PROXY_HOST_ELEMENT = "Host";
		private const string PROXY_PORT_ELEMENT = "Port";
		private const string PROXY_TYPE_ELEMENT = "Type";
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
		/// Gets/sets the configurable proxy type.
		/// </summary>
		public eProxyType? ProxyType { get; set; }

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
			ProxyType = null;
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
				writer.WriteElementString(PROXY_TYPE_ELEMENT, IcdXmlConvert.ToString(ProxyType));
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

			ProxyUsername = XmlUtils.TryReadChildElementContentAsString(proxy, PROXY_USERNAME_ELEMENT);
			ProxyPassword = XmlUtils.TryReadChildElementContentAsString(proxy, PROXY_PASSWORD_ELEMENT);
			ProxyHost = XmlUtils.TryReadChildElementContentAsString(proxy, PROXY_HOST_ELEMENT);
			ProxyPort = XmlUtils.TryReadChildElementContentAsUShort(proxy, PROXY_PORT_ELEMENT);
			ProxyType = XmlUtils.TryReadChildElementContentAsEnum<eProxyType>(proxy, PROXY_TYPE_ELEMENT, true);
			ProxyAuthenticationMethod = XmlUtils.TryReadChildElementContentAsEnum<eProxyAuthenticationMethod>(proxy, PROXY_METHOD_ELEMENT, true);
		}

		#endregion
	}
}
