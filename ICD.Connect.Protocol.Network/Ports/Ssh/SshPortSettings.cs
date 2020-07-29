using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
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
		private readonly List<PrivateKey> m_PrivateKeys; 

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

		/// <summary>
		/// Gets/sets the path to the first private key.
		/// </summary>
		[PublicAPI("DAV")]
		public string PrimaryPrivateKeyPath
		{
			get { return m_PrivateKeys.Select(pk => pk.Path).FirstOrDefault(); }
			set
			{
				if (m_PrivateKeys.Count == 0)
					m_PrivateKeys.Add(new PrivateKey());
				m_PrivateKeys[0].Path = value;
			}
		}

		/// <summary>
		/// Gets/sets the pass-phrase for the first private key.
		/// </summary>
		[PublicAPI("DAV")]
		public string PrimaryPrivateKeyPassPhrase
		{
			get { return m_PrivateKeys.Select(pk => pk.PassPhrase).FirstOrDefault(); }
			set
			{
				if (m_PrivateKeys.Count == 0)
					m_PrivateKeys.Add(new PrivateKey());
				m_PrivateKeys[0].PassPhrase = value;
			}
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

			m_PrivateKeys = new List<PrivateKey>();
		}

		#region Methods

		/// <summary>
		/// Gets the configured path -> pass-phrase pairs.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<PrivateKey> GetPrivateKeys()
		{
			return m_PrivateKeys.Where(pk => !string.IsNullOrEmpty(pk.Path))
			                    .ToArray(m_PrivateKeys.Count);
		}

		/// <summary>
		/// Sets the configured path -> pass-phrase pairs.
		/// </summary>
		/// <returns></returns>
		public void SetPrivateKeys(IEnumerable<PrivateKey> privateKeys)
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

			IEnumerable<KeyValuePair<string, string>> kvps =
				GetPrivateKeys().Select(pk => new KeyValuePair<string, string>(pk.Path, pk.PassPhrase));

			XmlUtils.WriteDictToXml(writer, kvps, PRIVATE_KEYS_ELEMENT, PRIVATE_KEY_ELEMENT, PATH_ELEMENT, PASSPHRASE_ELEMENT);
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			IEnumerable<PrivateKey> privateKeys =
				XmlUtils.ReadDictFromXml<string, string>(xml, PRIVATE_KEYS_ELEMENT, PRIVATE_KEY_ELEMENT, PATH_ELEMENT,
				                                         PASSPHRASE_ELEMENT)
				        .Select(kvp => new PrivateKey {Path = kvp.Key, PassPhrase = kvp.Value});

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
