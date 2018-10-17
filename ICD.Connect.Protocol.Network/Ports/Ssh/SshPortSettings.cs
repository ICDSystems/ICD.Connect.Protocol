using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Xml;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.Network.Ports.Ssh
{
	/// <summary>
	/// Provides a temporary store for SSH port settings.
	/// </summary>
	[KrangSettings("SSH", typeof(SshPort))]
	public sealed class SshPortSettings : AbstractSecureNetworkPortSettings
	{
		private const string PRIVATE_KEYS_ELEMENT = "PrivateKeys";
		private const string PRIVATE_KEY_ELEMENT = "PrivateKey";
		private const string PATH_ELEMENT = "Path";
		private const string PASSPHRASE_ELEMENT = "PassPhrase";

		private readonly SecureNetworkProperties m_NetworkProperties;

		/// <summary>
		/// Path -> Pass-phrase
		/// </summary>
		private readonly Dictionary<string, string> m_PrivateKeys; 

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
		public override ushort? NetworkPort
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

			m_PrivateKeys = new Dictionary<string, string>();
		}

		#region Methods

		/// <summary>
		/// Gets the configured path -> pass-phrase pairs.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<KeyValuePair<string, string>> GetPrivateKeys()
		{
			return m_PrivateKeys.ToArray(m_PrivateKeys.Count);
		}

		/// <summary>
		/// Sets the configured path -> pass-phrase pairs.
		/// </summary>
		/// <returns></returns>
		public void SetPrivateKeys(IEnumerable<KeyValuePair<string, string>> privateKeys)
		{
			m_PrivateKeys.Clear();
			m_PrivateKeys.AddRange(privateKeys);
		}

		/// <summary>
		/// Clears the configured values.
		/// </summary>
		public override void ClearNetworkProperties()
		{
			m_NetworkProperties.ClearNetworkProperties();
		}

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			XmlUtils.WriteDictToXml(writer, GetPrivateKeys(), PRIVATE_KEYS_ELEMENT, PRIVATE_KEY_ELEMENT, PATH_ELEMENT, PASSPHRASE_ELEMENT);
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			IEnumerable<KeyValuePair<string, string>> privateKeys =
				XmlUtils.ReadDictFromXml<string, string>(xml, PRIVATE_KEYS_ELEMENT, PRIVATE_KEY_ELEMENT, PATH_ELEMENT, PASSPHRASE_ELEMENT);

			SetPrivateKeys(privateKeys);

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
