using ICD.Common.Utils.Xml;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.IoT.Ports
{
	[KrangSettings("MQTT", typeof(IcdMqttClient))]
	public sealed class IcdMqttClientSettings : AbstractPortSettings
	{
		private const string ELEMENT_HOSTNAME = "Hostname";
		private const string ELEMENT_NETWORK_PORT = "Network Port";
		private const string ELEMENT_CLIENT_ID = "ClientId";
		private const string ELEMENT_USERNAME = "Username";
		private const string ELEMENT_PASSWORD = "Password";
		private const string ELEMENT_SECURE = "Secure";

		/// <summary>
		/// Gets/sets the hostname.
		/// </summary>
		public string Hostname { get; set; }

		public int? NetworkPort { get; set; }

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

		public bool Secure { get; set; }

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(ELEMENT_HOSTNAME, Hostname);
			writer.WriteElementString(ELEMENT_NETWORK_PORT, IcdXmlConvert.ToString(NetworkPort));
			writer.WriteElementString(ELEMENT_CLIENT_ID, ClientId);
			writer.WriteElementString(ELEMENT_USERNAME, Username);
			writer.WriteElementString(ELEMENT_PASSWORD, Password);
			writer.WriteElementString(ELEMENT_SECURE, IcdXmlConvert.ToString(Secure));
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Hostname = XmlUtils.TryReadChildElementContentAsString(xml, ELEMENT_HOSTNAME);
			NetworkPort = XmlUtils.TryReadChildElementContentAsInt(xml, ELEMENT_NETWORK_PORT);
			ClientId = XmlUtils.TryReadChildElementContentAsString(xml, ELEMENT_CLIENT_ID);
			Username = XmlUtils.TryReadChildElementContentAsString(xml, ELEMENT_USERNAME);
			Password = XmlUtils.TryReadChildElementContentAsString(xml, ELEMENT_PASSWORD);
			Secure = XmlUtils.TryReadChildElementContentAsBoolean(xml, ELEMENT_SECURE) ?? false;
		}
	}
}
