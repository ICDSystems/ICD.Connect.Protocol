using ICD.Common.Utils.Xml;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.Network.Ssh
{
	/// <summary>
	/// Provides a temporary store for SSH port settings.
	/// </summary>
	[KrangSettings("SSH", typeof(SshPort))]
	public sealed class SshPortSettings : AbstractSerialPortSettings, INetworkSettings
	{
		private ushort m_NetworkPort = SshPort.DEFAULT_PORT;

		#region Properties

		/// <summary>
		/// Gets/sets the configurable username.
		/// </summary>
		public string Username { get; set; }

		/// <summary>
		/// Gets/sets the configurable password.
		/// </summary>
		public string Password { get; set; }

		/// <summary>
		/// Gets/sets the configurable network address.
		/// </summary>
		public string NetworkAddress { get; set; }

		/// <summary>
		/// Gets/sets the configurable network port.
		/// </summary>
		public ushort NetworkPort { get { return m_NetworkPort; } set { m_NetworkPort = value; } }

		#endregion

		#region Method

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			NetworkSettingsParsing.WriteElements(writer, this);
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			NetworkSettingsParsing.ParseXml(xml, this);

			NetworkPort = NetworkPort == 0 ? SshPort.DEFAULT_PORT : NetworkPort;
		}

		#endregion
	}
}
