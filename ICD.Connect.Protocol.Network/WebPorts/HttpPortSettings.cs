using ICD.Common.Utils.Xml;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.Network.WebPorts
{
	/// <summary>
	/// Settings for a HttpPort.
	/// </summary>
	[KrangSettings("HTTP", typeof(HttpPort))]
	public sealed class HttpPortSettings : AbstractPortSettings, IUriProperties
	{
		private readonly UriProperties m_UriProperties;

		#region Properties

		/// <summary>
		/// Gets/sets the configurable username.
		/// </summary>
		public string Username { get { return m_UriProperties.Username; } set { m_UriProperties.Username = value; } }

		/// <summary>
		/// Gets/sets the configurable password.
		/// </summary>
		public string Password { get { return m_UriProperties.Password; } set { m_UriProperties.Password = value; } }

		/// <summary>
		/// Gets/sets the configurable network address.
		/// </summary>
		public string NetworkAddress { get { return m_UriProperties.NetworkAddress; } set { m_UriProperties.NetworkAddress = value; } }

		/// <summary>
		/// Gets/sets the configurable network port.
		/// </summary>
		public ushort NetworkPort { get { return m_UriProperties.NetworkPort; } set { m_UriProperties.NetworkPort = value; } }

		/// <summary>
		/// Gets/sets the configurable URI scheme.
		/// </summary>
		public string UriScheme { get { return m_UriProperties.UriScheme; } set { m_UriProperties.UriScheme = value; } }

		/// <summary>
		/// Gets/sets the configurable URI path.
		/// </summary>
		public string UriPath { get { return m_UriProperties.UriPath; } set { m_UriProperties.UriPath = value; } }

		/// <summary>
		/// Gets/sets the configurable URI query.
		/// </summary>
		public string UriQuery { get { return m_UriProperties.UriQuery; } set { m_UriProperties.UriQuery = value; } }

		/// <summary>
		/// Gets/sets the configurable URI fragment.
		/// </summary>
		public string UriFragment { get { return m_UriProperties.UriFragment; } set { m_UriProperties.UriFragment = value; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public HttpPortSettings()
		{
			m_UriProperties = new UriProperties
			{
				UriScheme = "http",
				NetworkPort = 80
			};
		}

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			m_UriProperties.WriteElements(writer);
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			m_UriProperties.ParseXml(xml);
		}
	}
}
