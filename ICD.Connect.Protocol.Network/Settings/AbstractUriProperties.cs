using System;
using ICD.Common.Utils.Xml;

namespace ICD.Connect.Protocol.Network.Settings
{
	public abstract class AbstractUriProperties : AbstractNetworkProperties, IUriProperties
	{
		private const string URI_SCHEME_ELEMENT = "UriScheme";
		private const string URI_PATH_ELEMENT = "UriPath";
		private const string URI_QUERY_ELEMENT = "UriQuery";
		private const string URI_FRAGMENT_ELEMENT = "UriFragment";

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

		/// <summary>
		/// Writes the username and password configuration to xml.
		/// </summary>
		/// <param name="writer"></param>
		public override void WriteElements(IcdXmlTextWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			writer.WriteElementString(URI_SCHEME_ELEMENT, UriScheme);
			writer.WriteElementString(URI_PATH_ELEMENT, UriPath);
			writer.WriteElementString(URI_QUERY_ELEMENT, UriQuery);
			writer.WriteElementString(URI_FRAGMENT_ELEMENT, UriFragment);

			base.WriteElements(writer);
		}

		/// <summary>
		/// Reads the username and password configuration from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			UriScheme = XmlUtils.TryReadChildElementContentAsString(xml, URI_SCHEME_ELEMENT);
			UriPath = XmlUtils.TryReadChildElementContentAsString(xml, URI_PATH_ELEMENT);
			UriQuery = XmlUtils.TryReadChildElementContentAsString(xml, URI_QUERY_ELEMENT);
			UriFragment = XmlUtils.TryReadChildElementContentAsString(xml, URI_FRAGMENT_ELEMENT);

			base.ParseXml(xml);
		}
	}
}
