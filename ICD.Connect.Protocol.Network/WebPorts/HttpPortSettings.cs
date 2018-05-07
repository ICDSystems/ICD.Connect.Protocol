using ICD.Common.Utils.Xml;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.Network.WebPorts
{
	/// <summary>
	/// Settings for a HttpPort.
	/// </summary>
	[KrangSettings("HTTP", typeof(HttpPort))]
	public sealed class HttpPortSettings : AbstractPortSettings
	{
		private const string ADDRESS_ELEMENT = "Address";
		private const string USERNAME_ELEMENT = "Username";
		private const string PASSWORD_ELEMENT = "Password";
		private const string ACCEPT_ELEMENT = "Accept";

		#region Properties

		public string Address { get; set; }

		public string Username { get; set; }

		public string Password { get; set; }

		public string Accept { get; set; }

		#endregion

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(ADDRESS_ELEMENT, Address);
			writer.WriteElementString(USERNAME_ELEMENT, Username);
			writer.WriteElementString(PASSWORD_ELEMENT, Password);
			writer.WriteElementString(ACCEPT_ELEMENT, Accept);
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Address = XmlUtils.TryReadChildElementContentAsString(xml, ADDRESS_ELEMENT);
			Username = XmlUtils.TryReadChildElementContentAsString(xml, USERNAME_ELEMENT);
			Password = XmlUtils.TryReadChildElementContentAsString(xml, PASSWORD_ELEMENT);
			Accept = XmlUtils.TryReadChildElementContentAsString(xml, ACCEPT_ELEMENT);
		}
	}
}
