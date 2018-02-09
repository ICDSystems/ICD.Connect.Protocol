using System;
using ICD.Common.Utils.Xml;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.Network.Ssh
{
	/// <summary>
	/// Provides a temporary store for SSH port settings.
	/// </summary>
	[KrangSettings(FACTORY_NAME)]
	public sealed class SshPortSettings : AbstractSerialPortSettings
	{
		private const string FACTORY_NAME = "SSH";

		private const string ADDRESS_ELEMENT = "Address";
		private const string HOST_PORT_ELEMENT = "Port";
		private const string USERNAME_ELEMENT = "Username";
		private const string PASSWORD_ELEMENT = "Password";

		private ushort m_Port = SshPort.DEFAULT_PORT;

		#region Properties

		public string Address { get; set; }
		public string Username { get; set; }
		public string Password { get; set; }
		public ushort Port { get { return m_Port; } set { m_Port = value; } }

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		/// <summary>
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(SshPort); } }

		#endregion

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

			string address = XmlUtils.TryReadChildElementContentAsString(xml, ADDRESS_ELEMENT);
			string username = XmlUtils.TryReadChildElementContentAsString(xml, USERNAME_ELEMENT);
			string password = XmlUtils.TryReadChildElementContentAsString(xml, PASSWORD_ELEMENT);
			ushort? port = XmlUtils.TryReadChildElementContentAsUShort(xml, HOST_PORT_ELEMENT);

			Address = address;
			Username = username;
			Password = password;
			Port = port ?? 0;
		}

		#endregion
	}
}
