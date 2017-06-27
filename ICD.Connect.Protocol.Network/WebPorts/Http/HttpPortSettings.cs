using System;
using ICD.Common.Properties;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Attributes.Factories;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Protocol.Network.WebPorts.Http
{
	/// <summary>
	/// Settings for a HttpPort.
	/// </summary>
	public sealed class HttpPortSettings : AbstractWebPortSettings
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

		/// <summary>
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlPortSettingsFactoryMethod(FACTORY_NAME)]
		public static HttpPortSettings FromXml(string xml)
		{
			HttpPortSettings output = new HttpPortSettings();
			ParseXml(output, xml);
			return output;
		}
	}
}
