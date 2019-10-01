﻿using ICD.Common.Utils.Xml;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.Network.Ports.Web
{
	public abstract class AbstractWebPortSettings : AbstractPortSettings, IWebPortSettings
	{
		#region URI

		private readonly UriProperties m_UriProperties;

		/// <summary>
		/// Gets/sets the configurable URI username.
		/// </summary>
		public string UriUsername { get { return m_UriProperties.UriUsername; } set { m_UriProperties.UriUsername = value; } }

		/// <summary>
		/// Gets/sets the configurable URI password.
		/// </summary>
		public string UriPassword { get { return m_UriProperties.UriPassword; } set { m_UriProperties.UriPassword = value; } }

		/// <summary>
		/// Gets/sets the configurable URI host.
		/// </summary>
		public string UriHost { get { return m_UriProperties.UriHost; } set { m_UriProperties.UriHost = value; } }

		/// <summary>
		/// Gets/sets the configurable URI port.
		/// </summary>
		public ushort? UriPort { get { return m_UriProperties.UriPort; } set { m_UriProperties.UriPort = value; } }

		/// <summary>
		/// Gets/sets the configurable URI scheme.
		/// </summary>
		public string UriScheme { get { return m_UriProperties.UriScheme; } set { m_UriProperties.UriScheme = value; } }

		/// <summary>
		/// Gets/sets the configurable URI path.
		/// </summary>
		public string UriPath { get { return m_UriProperties.UriPath; } set { m_UriProperties.UriPath = value; } }

		/// <summary>
		/// Gets/sets the configurable URI query.
		/// </summary>
		public string UriQuery { get { return m_UriProperties.UriQuery; } set { m_UriProperties.UriQuery = value; } }

		/// <summary>
		/// Gets/sets the configurable URI fragment.
		/// </summary>
		public string UriFragment { get { return m_UriProperties.UriFragment; } set { m_UriProperties.UriFragment = value; } }

		/// <summary>
		/// Clears the configured values.
		/// </summary>
		void IUriProperties.ClearUriProperties()
		{
			m_UriProperties.ClearUriProperties();
		}

		#endregion

		#region Proxy

		private readonly ProxyProperties m_ProxyProperties;

		/// <summary>
		/// Gets/sets the configurable proxy username.
		/// </summary>
		public string ProxyUsername { get { return m_ProxyProperties.ProxyUsername; } set { m_ProxyProperties.ProxyUsername = value; } }

		/// <summary>
		/// Gets/sets the configurable proxy password.
		/// </summary>
		public string ProxyPassword { get { return m_ProxyProperties.ProxyPassword; } set { m_ProxyProperties.ProxyPassword = value; } }

		/// <summary>
		/// Gets/sets the configurable proxy host.
		/// </summary>
		public string ProxyHost { get { return m_ProxyProperties.ProxyHost; } set { m_ProxyProperties.ProxyHost = value; } }

		/// <summary>
		/// Gets/sets the configurable proxy port.
		/// </summary>
		public ushort? ProxyPort { get { return m_ProxyProperties.ProxyPort; } set { m_ProxyProperties.ProxyPort = value; } }

		/// <summary>
		/// Gets/sets the configurable proxy scheme.
		/// </summary>
		public string ProxyScheme { get { return m_ProxyProperties.ProxyScheme; } set { m_ProxyProperties.ProxyScheme = value; } }

		/// <summary>
		/// Gets/sets the configurable proxy authentication method.
		/// </summary>
		public eProxyAuthenticationMethod? ProxyAuthenticationMethod
		{
			get { return m_ProxyProperties.ProxyAuthenticationMethod; }
			set { m_ProxyProperties.ProxyAuthenticationMethod = value; }
		}

		/// <summary>
		/// Clears the configured values.
		/// </summary>
		public void ClearProxyProperties()
		{
			m_ProxyProperties.ClearProxyProperties();
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractWebPortSettings()
		{
			m_UriProperties = new UriProperties();
			m_ProxyProperties = new ProxyProperties();
		}

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			m_UriProperties.WriteElements(writer);
			m_ProxyProperties.WriteElements(writer);
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			m_UriProperties.ParseXml(xml);
			m_ProxyProperties.ParseXml(xml);
		}
	}
}
