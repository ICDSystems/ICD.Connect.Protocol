#if !SIMPLSHARP
using System;
using System.IO.Pipes;
using System.Security.Principal;
using ICD.Common.Utils.Xml;

namespace ICD.Connect.Protocol.Network.Ports.NamedPipe
{
	public sealed class NamedPipeProperties : INamedPipeProperties
	{
		private const string ELEMENT = "NamedPipe";

		private const string ELEMENT_HOSTNAME = "Hostname";
		private const string ELEMENT_NAME = "Name";
		private const string ELEMENT_DIRECTION = "Direction";
		private const string ELEMENT_OPTIONS = "Options";
		private const string ELEMENT_TOKEN_IMPERSONATION = "TokenImpersonation";

		#region Properties

		/// <summary>
		/// Gets/sets the configurable remote hostname.
		/// </summary>
		public string NamedPipeHostname { get; set; }

		/// <summary>
		/// Gets/sets the configurable pipe name.
		/// </summary>
		public string NamedPipeName { get; set; }

		/// <summary>
		/// Gets/sets the configurable pipe direction.
		/// </summary>
		public PipeDirection? NamedPipeDirection { get; set; }

		/// <summary>
		/// Gets/sets the configurable pipe options.
		/// </summary>
		public PipeOptions? NamedPipeOptions { get; set; }

		/// <summary>
		/// Gets/sets the configurable token impersonation level.
		/// </summary>
		public TokenImpersonationLevel? NamedPipeTokenImpersonationLevel { get; set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public NamedPipeProperties()
		{
			ClearNamedPipeProperties();
		}

		#region Methods

		/// <summary>
		/// Clears the configured values.
		/// </summary>
		public void ClearNamedPipeProperties()
		{
			NamedPipeHostname = null;
			NamedPipeName = null;
			NamedPipeDirection = null;
			NamedPipeOptions = null;
			NamedPipeTokenImpersonationLevel = null;
		}

		/// <summary>
		/// Writes the configuration to xml.
		/// </summary>
		/// <param name="writer"></param>
		public void WriteElements(IcdXmlTextWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			writer.WriteStartElement(ELEMENT);
			{
				writer.WriteElementString(ELEMENT_HOSTNAME, NamedPipeHostname);
				writer.WriteElementString(ELEMENT_NAME, NamedPipeName);
				writer.WriteElementString(ELEMENT_DIRECTION, IcdXmlConvert.ToString(NamedPipeDirection));
				writer.WriteElementString(ELEMENT_OPTIONS, IcdXmlConvert.ToString(NamedPipeOptions));
				writer.WriteElementString(ELEMENT_TOKEN_IMPERSONATION, IcdXmlConvert.ToString(NamedPipeTokenImpersonationLevel));
			}
			writer.WriteEndElement();
		}

		/// <summary>
		/// Reads the configuration from xml.
		/// </summary>
		/// <param name="xml"></param>
		public void ParseXml(string xml)
		{
			ClearNamedPipeProperties();

			string configXml;
			if (!XmlUtils.TryGetChildElementAsString(xml, ELEMENT, out configXml))
				return;

			NamedPipeDirection = XmlUtils.TryReadChildElementContentAsEnum<PipeDirection>(configXml, ELEMENT_DIRECTION, true);
			NamedPipeOptions = XmlUtils.TryReadChildElementContentAsEnum<PipeOptions>(configXml, ELEMENT_OPTIONS, true);
			NamedPipeTokenImpersonationLevel = XmlUtils.TryReadChildElementContentAsEnum<TokenImpersonationLevel>(configXml, ELEMENT_TOKEN_IMPERSONATION, true);

			string hostname = XmlUtils.TryReadChildElementContentAsString(configXml, ELEMENT_HOSTNAME);
			string name = XmlUtils.TryReadChildElementContentAsString(configXml, ELEMENT_NAME);

			// If strings are empty, set the value as null so overrides will work properly
			NamedPipeHostname = string.IsNullOrEmpty(hostname) ? null : hostname;
			NamedPipeName = string.IsNullOrEmpty(name) ? null : name;
		}

		#endregion
	}
}
#endif
