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
	public sealed class HttpPortSettings : AbstractPortSettings, IUriSettings
	{
		#region Properties

		/// <summary>
		/// Gets/sets the configurable username.
		/// </summary>
		public string UserName { get; set; }

		/// <summary>
		/// Gets/sets the configurable password.
		/// </summary>
		public string Password { get; set; }

		/// <summary>
		/// Gets/sets the configurable network address.
		/// </summary>
		public string NetworkAddress { get; set; }

		/// <summary>
		/// Gets/sets the configurable network port.
		/// </summary>
		public ushort NetworkPort { get; set; }

		/// <summary>
		/// Gets/sets the configurable URI scheme.
		/// </summary>
		public string UriScheme { get; set; }

		/// <summary>
		/// Gets/sets the configurable URI path.
		/// </summary>
		public string UriPath { get; set; }

		/// <summary>
		/// Gets/sets the configurable URI query.
		/// </summary>
		public string UriQuery { get; set; }

		/// <summary>
		/// Gets/sets the configurable URI fragment.
		/// </summary>
		public string UriFragment { get; set; }

		#endregion

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			UriSettingsParsing.WriteElements(writer, this);
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			UriSettingsParsing.ParseXml(xml, this);
		}
	}
}
