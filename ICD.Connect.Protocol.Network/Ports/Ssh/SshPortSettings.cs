using ICD.Common.Utils.Xml;
using ICD.Connect.Protocol.Network.Ports;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.Network.Ssh
{
	/// <summary>
	/// Provides a temporary store for SSH port settings.
	/// </summary>
	[KrangSettings("SSH", typeof(SshPort))]
	public sealed class SshPortSettings : AbstractSecureNetworkPortSettings
	{
		private readonly SecureNetworkProperties m_NetworkProperties;

		#region Properties

		/// <summary>
		/// Gets/sets the configurable network username.
		/// </summary>
		public override string NetworkUsername
		{
			get { return m_NetworkProperties.NetworkUsername; }
			set { m_NetworkProperties.NetworkUsername = value; }
		}

		/// <summary>
		/// Gets/sets the configurable network password.
		/// </summary>
		public override string NetworkPassword
		{
			get { return m_NetworkProperties.NetworkPassword; }
			set { m_NetworkProperties.NetworkPassword = value; }
		}

		/// <summary>
		/// Gets/sets the configurable network address.
		/// </summary>
		public override string NetworkAddress
		{
			get { return m_NetworkProperties.NetworkAddress; }
			set { m_NetworkProperties.NetworkAddress = value; }
		}

		/// <summary>
		/// Gets/sets the configurable network port.
		/// </summary>
		public override ushort NetworkPort
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
			m_NetworkProperties = new SecureNetworkProperties
			{
				NetworkPort = SshPort.DEFAULT_PORT
			};
		}

		#region Methods

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			NetworkPort = NetworkPort == 0 ? SshPort.DEFAULT_PORT : NetworkPort;
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Override to serialize the network configuration to XML.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteNetworkElements(IcdXmlTextWriter writer)
		{
			m_NetworkProperties.WriteElements(writer);
		}

		/// <summary>
		/// Override to deserialize the network configuration from XML.
		/// </summary>
		/// <param name="xml"></param>
		protected override void ParseNetworkElements(string xml)
		{
			m_NetworkProperties.ParseXml(xml);
		}

		#endregion
	}
}
