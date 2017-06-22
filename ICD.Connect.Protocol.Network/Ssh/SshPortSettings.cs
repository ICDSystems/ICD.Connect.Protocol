using ICD.Common.Properties;
using ICD.Common.Utils.Xml;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Attributes.Factories;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Protocol.Network.Ssh
{
	/// <summary>
	/// Provides a temporary store for SSH port settings.
	/// </summary>
	public sealed class SshPortSettings : AbstractPortSettings
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
		/// Creates a new originator instance from the settings.
		/// </summary>
		/// <param name="factory"></param>
		/// <returns></returns>
		public override IOriginator ToOriginator(IDeviceFactory factory)
		{
			SshPort output = new SshPort();
			output.ApplySettings(this, factory);

			return output;
		}

		/// <summary>
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlPortSettingsFactoryMethod(FACTORY_NAME)]
		public static SshPortSettings FromXml(string xml)
		{
			string address = XmlUtils.ReadChildElementContentAsString(xml, ADDRESS_ELEMENT);
			string username = XmlUtils.ReadChildElementContentAsString(xml, USERNAME_ELEMENT);
			string password = XmlUtils.ReadChildElementContentAsString(xml, PASSWORD_ELEMENT);
			int? port = XmlUtils.TryReadChildElementContentAsInt(xml, HOST_PORT_ELEMENT);

			SshPortSettings output = new SshPortSettings
			{
				Address = address,
				Username = username,
				Password = password
			};

			if (port != null)
				output.Port = (ushort)port;

			ParseXml(output, xml);
			return output;
		}

		#endregion
	}
}
