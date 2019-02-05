﻿using System;
using ICD.Common.Utils.Xml;

namespace ICD.Connect.Protocol.Network.Settings
{
	public abstract class AbstractUriProperties : IUriProperties
	{
		private const string ELEMENT = "Uri";

		private const string URI_USERNAME_ELEMENT = "Username";
		private const string URI_PASSWORD_ELEMENT = "Password";
		private const string URI_HOST_ELEMENT = "Host";
		private const string URI_PORT_ELEMENT = "Port";
		private const string URI_SCHEME_ELEMENT = "Scheme";
		private const string URI_PATH_ELEMENT = "Path";
		private const string URI_QUERY_ELEMENT = "Query";
		private const string URI_FRAGMENT_ELEMENT = "Fragment";

		#region Properties

		/// <summary>
		/// Gets/sets the configurable URI username.
		/// </summary>
		public string UriUsername { get; set; }

		/// <summary>
		/// Gets/sets the configurable URI password.
		/// </summary>
		public string UriPassword { get; set; }

		/// <summary>
		/// Gets/sets the configurable URI host.
		/// </summary>
		public string UriHost { get; set; }

		/// <summary>
		/// Gets/sets the configurable URI port.
		/// </summary>
		public ushort? UriPort { get; set; }

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
		/// Constructor.
		/// </summary>
		protected AbstractUriProperties()
		{
			Clear();
		}

		#region Methods

		/// <summary>
		/// Clears the configured properties.
		/// </summary>
		public void Clear()
		{
			UriUsername = null;
			UriPassword = null;
			UriHost = null;
			UriPort = null;
			UriScheme = null;
			UriPath = null;
			UriQuery = null;
			UriFragment = null;
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
				writer.WriteElementString(URI_SCHEME_ELEMENT, UriScheme);
				writer.WriteElementString(URI_USERNAME_ELEMENT, UriUsername);
				writer.WriteElementString(URI_PASSWORD_ELEMENT, UriPassword);
				writer.WriteElementString(URI_HOST_ELEMENT, UriHost);
				writer.WriteElementString(URI_PORT_ELEMENT, IcdXmlConvert.ToString(UriPort));
				writer.WriteElementString(URI_PATH_ELEMENT, UriPath);
				writer.WriteElementString(URI_QUERY_ELEMENT, UriQuery);
				writer.WriteElementString(URI_FRAGMENT_ELEMENT, UriFragment);
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

			string uri;
			if (!XmlUtils.TryGetChildElementAsString(xml, ELEMENT, out uri))
				return;

			UriScheme = XmlUtils.TryReadChildElementContentAsString(uri, URI_SCHEME_ELEMENT);
			UriUsername = XmlUtils.TryReadChildElementContentAsString(uri, URI_USERNAME_ELEMENT);
			UriPassword = XmlUtils.TryReadChildElementContentAsString(uri, URI_PASSWORD_ELEMENT);
			UriHost = XmlUtils.TryReadChildElementContentAsString(uri, URI_HOST_ELEMENT);
			UriPort = XmlUtils.TryReadChildElementContentAsUShort(uri, URI_PORT_ELEMENT);
			UriPath = XmlUtils.TryReadChildElementContentAsString(uri, URI_PATH_ELEMENT);
			UriQuery = XmlUtils.TryReadChildElementContentAsString(uri, URI_QUERY_ELEMENT);
			UriFragment = XmlUtils.TryReadChildElementContentAsString(uri, URI_FRAGMENT_ELEMENT);
		}

		#endregion
	}
}