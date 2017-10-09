using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.Xml;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.Network.WebPorts
{
	/// <summary>
	/// Settings for a HttpPort.
	/// </summary>
	public sealed class HttpPortSettings : AbstractPortSettings
	{
		private const string FACTORY_NAME = "HTTP";

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		/// <summary>
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(HttpPort); } }

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

		#endregion

		/// <summary>
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlFactoryMethod(FACTORY_NAME)]
		public static HttpPortSettings FromXml(string xml)
		{
			HttpPortSettings output = new HttpPortSettings
			{
				Address = XmlUtils.TryReadChildElementContentAsString(xml, ADDRESS_ELEMENT),
				Username = XmlUtils.TryReadChildElementContentAsString(xml, USERNAME_ELEMENT),
				Password = XmlUtils.TryReadChildElementContentAsString(xml, PASSWORD_ELEMENT),
				Accept = XmlUtils.TryReadChildElementContentAsString(xml, ACCEPT_ELEMENT)
			};

			ParseXml(output, xml);
			return output;
		}
	}
}
