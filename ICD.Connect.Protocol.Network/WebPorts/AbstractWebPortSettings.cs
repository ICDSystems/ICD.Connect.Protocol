using System.Collections.Generic;
using ICD.Common.Utils.Xml;
using ICD.Connect.Settings;

namespace ICD.Connect.Protocol.Network.WebPorts
{
	/// <summary>
	/// Base class for web port settings.
	/// </summary>
	public abstract class AbstractWebPortSettings : AbstractPortSettings
	{
		private const string ADDRESS_ELEMENT = "Address";
		private const string USERNAME_ELEMENT = "Username";
		private const string PASSWORD_ELEMENT = "Password";
		private const string ACCEPT_ELEMENT = "Accept";

		#region Properties

		public string Address { get; set; }
		public string Username { get; set; }
		public string Password { get; set; }
		public string Accept { get; set; }

		#endregion

		#region Methods

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			if (!string.IsNullOrEmpty(Address))
				writer.WriteElementString(ADDRESS_ELEMENT, Address);

			if (!string.IsNullOrEmpty(Username))
				writer.WriteElementString(USERNAME_ELEMENT, Username);

			if (!string.IsNullOrEmpty(Password))
				writer.WriteElementString(PASSWORD_ELEMENT, Password);

			if (!string.IsNullOrEmpty(Accept))
				writer.WriteElementString(ACCEPT_ELEMENT, Accept);
		}

		/// <summary>
		/// Returns the collection of ids that the settings will depend on.
		/// For example, to instantiate an IR Port from settings, the device the physical port
		/// belongs to will need to be instantiated first.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<int> GetDeviceDependencies()
		{
			yield break;
		}

		/// <summary>
		/// Parses the xml and applies the properties to the instance.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="xml"></param>
		protected static void ParseXml(AbstractWebPortSettings instance, string xml)
		{
			instance.Address = XmlUtils.TryReadChildElementContentAsString(xml, ADDRESS_ELEMENT);
			instance.Username = XmlUtils.TryReadChildElementContentAsString(xml, USERNAME_ELEMENT);
			instance.Password = XmlUtils.TryReadChildElementContentAsString(xml, PASSWORD_ELEMENT);
			instance.Accept = XmlUtils.TryReadChildElementContentAsString(xml, ACCEPT_ELEMENT);

			AbstractDeviceSettings.ParseXml(instance, xml);
		}

		#endregion
	}
}
