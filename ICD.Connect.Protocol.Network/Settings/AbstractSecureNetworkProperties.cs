using ICD.Common.Utils.Xml;

namespace ICD.Connect.Protocol.Network.Settings
{
	public abstract class AbstractSecureNetworkProperties : AbstractNetworkProperties, ISecureNetworkProperties
	{
		private const string NETWORK_USERNAME_ELEMENT = "Username";
		private const string NETWORK_PASSWORD_ELEMENT = "Password";

		#region Properties

		/// <summary>
		/// Gets/sets the configurable network username.
		/// </summary>
		public string NetworkUsername { get; set; }

		/// <summary>
		/// Gets/sets the configurable network password.
		/// </summary>
		public string NetworkPassword { get; set; }

		#endregion

		#region Methods

		/// <summary>
		/// Clears the configured properties.
		/// </summary>
		public override void ClearNetworkProperties()
		{
			base.ClearNetworkProperties();

			NetworkUsername = null;
			NetworkPassword = null;
		}

		/// <summary>
		/// Override to write additional elements to XML.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteInnerElements(IcdXmlTextWriter writer)
		{
			base.WriteInnerElements(writer);

			writer.WriteElementString(NETWORK_USERNAME_ELEMENT, NetworkUsername);
			writer.WriteElementString(NETWORK_PASSWORD_ELEMENT, NetworkPassword);
		}

		/// <summary>
		/// Override to parse additional elements from XML.
		/// </summary>
		/// <param name="xml"></param>
		protected override void ParseInnerXml(string xml)
		{
			base.ParseInnerXml(xml);

			string networkUsername = XmlUtils.TryReadChildElementContentAsString(xml, NETWORK_USERNAME_ELEMENT);
			string networkPassword = XmlUtils.TryReadChildElementContentAsString(xml, NETWORK_PASSWORD_ELEMENT);

			// If strings are empty, set the value as null so overrides will work properly
			NetworkUsername = string.IsNullOrEmpty(networkUsername) ? null : networkUsername.Trim();
			NetworkPassword = string.IsNullOrEmpty(networkPassword) ? null : networkPassword.Trim();
		}

		#endregion
	}
}
