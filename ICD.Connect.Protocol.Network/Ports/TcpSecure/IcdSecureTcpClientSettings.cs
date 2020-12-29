using ICD.Common.Utils.Xml;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.Network.Ports.TcpSecure
{
	[KrangSettings("SecureTCP", typeof(IcdSecureTcpClient))]
	public sealed class IcdSecureTcpClientSettings : AbstractNetworkPortSettings
	{
		private readonly NetworkProperties m_NetworkProperties;

		#region Properties

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
		public override ushort? NetworkPort
		{
			get { return m_NetworkProperties.NetworkPort; }
			set { m_NetworkProperties.NetworkPort = value; }
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public IcdSecureTcpClientSettings()
		{
			m_NetworkProperties = new NetworkProperties();
		}

		#region Methods

		/// <summary>
		/// Clears the configured values.
		/// </summary>
		public override void ClearNetworkProperties()
		{
			m_NetworkProperties.ClearNetworkProperties();
		}

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