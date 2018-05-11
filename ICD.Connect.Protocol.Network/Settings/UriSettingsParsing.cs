using System;
using ICD.Common.Utils.Xml;

namespace ICD.Connect.Protocol.Network.Settings
{
	public static class UriSettingsParsing
	{
		private const string URI_SCHEME_ELEMENT = "UriScheme";
		private const string URI_PATH_ELEMENT = "UriPath";
		private const string URI_QUERY_ELEMENT = "UriQuery";
		private const string URI_FRAGMENT_ELEMENT = "UriFragment";

		/// <summary>
		/// Writes the username and password configuration to xml.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="instance"></param>
		public static void WriteElements(IcdXmlTextWriter writer, IUriSettings instance)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			if (instance == null)
				throw new ArgumentNullException("instance");

			writer.WriteElementString(URI_SCHEME_ELEMENT, instance.UriScheme);
			writer.WriteElementString(URI_PATH_ELEMENT, instance.UriPath);
			writer.WriteElementString(URI_QUERY_ELEMENT, instance.UriQuery);
			writer.WriteElementString(URI_FRAGMENT_ELEMENT, instance.UriFragment);

			NetworkSettingsParsing.WriteElements(writer, instance);
		}

		/// <summary>
		/// Reads the username and password configuration from xml.
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="instance"></param>
		public static void ParseXml(string xml, IUriSettings instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			instance.UriScheme = XmlUtils.TryReadChildElementContentAsString(xml, URI_SCHEME_ELEMENT);
			instance.UriPath = XmlUtils.TryReadChildElementContentAsString(xml, URI_PATH_ELEMENT);
			instance.UriQuery = XmlUtils.TryReadChildElementContentAsString(xml, URI_QUERY_ELEMENT);
			instance.UriFragment = XmlUtils.TryReadChildElementContentAsString(xml, URI_FRAGMENT_ELEMENT);

			NetworkSettingsParsing.ParseXml(xml, instance);
		}
	}
}
