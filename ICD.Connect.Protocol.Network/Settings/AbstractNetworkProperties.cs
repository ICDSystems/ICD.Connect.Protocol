using System;
using ICD.Common.Utils.Xml;

namespace ICD.Connect.Protocol.Network.Settings
{
	public abstract class AbstractNetworkProperties : INetworkProperties
	{
		private const string ELEMENT = "Network";

		private const string NETWORK_ADDRESS_ELEMENT = "Address";
		private const string NETWORK_PORT_ELEMENT = "Port";
		private const string NETWORK_USERNAME_ELEMENT = "Username";
		private const string NETWORK_PASSWORD_ELEMENT = "Password";

		#region Properties

		/// <summary>
		/// Gets/sets the configurable network address.
		/// </summary>
		public string NetworkAddress { get; set; }

		/// <summary>
		/// Gets/sets the configurable network port.
		/// </summary>
		public ushort NetworkPort { get; set; }

		/// <summary>
		/// Gets/sets the configurable network username.
		/// </summary>
		public string NetworkUsername { get; set; }

		/// <summary>
		/// Gets/sets the configurable network password.
		/// </summary>
		public string NetworkPassword { get; set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractNetworkProperties()
		{
			Clear();
		}

		#region Methods

		/// <summary>
		/// Clears the configured properties.
		/// </summary>
		public void Clear()
		{
			NetworkAddress = null;
			NetworkPort = 0;
			NetworkUsername = null;
			NetworkPassword = null;
		}

		/// <summary>
		/// Writes the username and password configuration to xml.
		/// </summary>
		/// <param name="writer"></param>
		public void WriteElements(IcdXmlTextWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			writer.WriteStartElement(ELEMENT);
			{
				writer.WriteElementString(NETWORK_ADDRESS_ELEMENT, NetworkAddress);
				writer.WriteElementString(NETWORK_PORT_ELEMENT, IcdXmlConvert.ToString(NetworkPort));
				writer.WriteElementString(NETWORK_USERNAME_ELEMENT, NetworkUsername);
				writer.WriteElementString(NETWORK_PASSWORD_ELEMENT, NetworkPassword);
			}
			writer.WriteEndElement();
		}

		/// <summary>
		/// Reads the username and password configuration from xml.
		/// </summary>
		/// <param name="xml"></param>
		public void ParseXml(string xml)
		{
			Clear();

			string networking;
			if (!XmlUtils.TryGetChildElementAsString(xml, ELEMENT, out networking))
				return;

			NetworkAddress = XmlUtils.TryReadChildElementContentAsString(networking, NETWORK_ADDRESS_ELEMENT);
			NetworkPort = XmlUtils.TryReadChildElementContentAsUShort(networking, NETWORK_PORT_ELEMENT) ?? 0;
			NetworkUsername = XmlUtils.TryReadChildElementContentAsString(networking, NETWORK_USERNAME_ELEMENT);
			NetworkPassword = XmlUtils.TryReadChildElementContentAsString(networking, NETWORK_PASSWORD_ELEMENT);
		}

		#endregion
	}
}
