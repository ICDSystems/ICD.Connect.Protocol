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
	public sealed class SshPortSettings : AbstractSerialPortSettings, INetworkProperties
	{
		private readonly NetworkProperties m_NetworkProperties;

		#region Properties

		/// <summary>
		/// Gets/sets the configurable username.
		/// </summary>
		public string Username { get { return m_NetworkProperties.Username; } set { m_NetworkProperties.Username = value; } }

		/// <summary>
		/// Gets/sets the configurable password.
		/// </summary>
		public string Password { get { return m_NetworkProperties.Password; } set { m_NetworkProperties.Password = value; } }

		/// <summary>
		/// Gets/sets the configurable network address.
		/// </summary>
		public string NetworkAddress
		{
			get { return m_NetworkProperties.NetworkAddress; }
			set { m_NetworkProperties.NetworkAddress = value; }
		}

		/// <summary>
		/// Gets/sets the configurable network port.
		/// </summary>
		public ushort NetworkPort
		{
			get { return m_NetworkProperties.NetworkPort; }
			set { m_NetworkProperties.NetworkPort = value; }
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public SshPortSettings()
		{
			m_NetworkProperties = new NetworkProperties
			{
				NetworkPort = SshPort.DEFAULT_PORT
			};
		}

		#region Method

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			m_NetworkProperties.WriteElements(writer);
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			m_NetworkProperties.ParseXml(xml);

			NetworkPort = NetworkPort == 0 ? SshPort.DEFAULT_PORT : NetworkPort;
		}

		#endregion
	}
}
