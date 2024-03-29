﻿using ICD.Common.Utils.Xml;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.Network.Ports.Mqtt
{
	public abstract class AbstractMqttClientSettings : AbstractConnectablePortSettings, IMqttClientSettings
	{
		private const string ELEMENT_HOSTNAME = "Hostname";
		private const string ELEMENT_PORT = "Port";
		private const string ELEMENT_PROXY_HOSTNAME = "ProxyHostname";
		private const string ELEMENT_PROXY_PORT = "ProxyPort";
		private const string ELEMENT_CLIENT_ID = "ClientId";
		private const string ELEMENT_USERNAME = "Username";
		private const string ELEMENT_PASSWORD = "Password";
		private const string ELEMENT_SECURE = "Secure";
		private const string ELEMENT_CA_CERT_PATH = "CaCertPath";

		/// <summary>
		/// Gets/sets the hostname.
		/// </summary>
		public string Hostname { get; set; }

		/// <summary>
		/// Gets/sets the network port.
		/// </summary>
		public ushort Port { get; set; }

		/// <summary>
		/// Gets/sets the proxy hostname.
		/// </summary>
		public string ProxyHostname { get; set; }

		/// <summary>
		/// Gets/sets the proxy port.
		/// </summary>
		public ushort ProxyPort { get; set; }

		/// <summary>
		/// Gets/sets the client id.
		/// </summary>
		public string ClientId { get; set; }

		/// <summary>
		/// Gets/sets the username.
		/// </summary>
		public string Username { get; set; }

		/// <summary>
		/// Gets/sets the password.
		/// </summary>
		public string Password { get; set; }

		/// <summary>
		/// Gets/sets the secure mode.
		/// </summary>
		public bool Secure { get; set; }

		/// <summary>
		/// Gets/sets the path to the certificate-authority certificate.
		/// </summary>
		public string CaCertPath { get; set; }

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(ELEMENT_HOSTNAME, Hostname);
			writer.WriteElementString(ELEMENT_PORT, IcdXmlConvert.ToString(Port));
			writer.WriteElementString(ELEMENT_PROXY_HOSTNAME, ProxyHostname);
			writer.WriteElementString(ELEMENT_PROXY_PORT, IcdXmlConvert.ToString(ProxyPort));
			writer.WriteElementString(ELEMENT_CLIENT_ID, ClientId);
			writer.WriteElementString(ELEMENT_USERNAME, Username);
			writer.WriteElementString(ELEMENT_PASSWORD, Password);
			writer.WriteElementString(ELEMENT_SECURE, IcdXmlConvert.ToString(Secure));
			writer.WriteElementString(ELEMENT_CA_CERT_PATH, CaCertPath);
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Hostname = XmlUtils.TryReadChildElementContentAsString(xml, ELEMENT_HOSTNAME);
			Port = XmlUtils.TryReadChildElementContentAsUShort(xml, ELEMENT_PORT) ?? 1883; // 1883 default network port for mqtt;
			ProxyHostname = XmlUtils.TryReadChildElementContentAsString(xml, ELEMENT_PROXY_HOSTNAME);
			ProxyPort = XmlUtils.TryReadChildElementContentAsUShort(xml, ELEMENT_PROXY_PORT) ?? 0;
			ClientId = XmlUtils.TryReadChildElementContentAsString(xml, ELEMENT_CLIENT_ID);
			Username = XmlUtils.TryReadChildElementContentAsString(xml, ELEMENT_USERNAME);
			Password = XmlUtils.TryReadChildElementContentAsString(xml, ELEMENT_PASSWORD);
			Secure = XmlUtils.TryReadChildElementContentAsBoolean(xml, ELEMENT_SECURE) ?? false;
			CaCertPath = XmlUtils.TryReadChildElementContentAsString(xml, ELEMENT_CA_CERT_PATH);
		}
	}
}
