using ICD.Common.Utils.Xml;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.Network.Ssh
{
	/// <summary>
	/// Provides a temporary store for SSH port settings.
	/// </summary>
	[KrangSettings("SSH", typeof(SshPort))]
	public sealed class SshPortSettings : AbstractSerialPortSettings
	{
		private const string ADDRESS_ELEMENT = "Address";
		private const string HOST_PORT_ELEMENT = "Port";
		private const string USERNAME_ELEMENT = "Username";
		private const string PASSWORD_ELEMENT = "Password";

		#region Properties

		public string Address { get; set; }

		public string Username { get; set; }

		public string Password { get; set; }

		public ushort Port { get; set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public SshPortSettings()
		{
			Port = SshPort.DEFAULT_PORT;
		}

		#region Method

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(ADDRESS_ELEMENT, Address);
			writer.WriteElementString(HOST_PORT_ELEMENT, IcdXmlConvert.ToString(Port));
			writer.WriteElementString(USERNAME_ELEMENT, Username);
			writer.WriteElementString(PASSWORD_ELEMENT, Password);
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
			Port = XmlUtils.TryReadChildElementContentAsUShort(xml, HOST_PORT_ELEMENT) ?? SshPort.DEFAULT_PORT;
		}

		#endregion
	}
}
