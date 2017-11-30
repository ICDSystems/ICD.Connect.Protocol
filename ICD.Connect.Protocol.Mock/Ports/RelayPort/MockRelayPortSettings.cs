using System;
using ICD.Common.Properties;
using ICD.Connect.Protocol.Ports.RelayPort;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.Mock.Ports.RelayPort
{
	public sealed class MockRelayPortSettings : AbstractRelayPortSettings
	{
		private const string FACTORY_NAME = "MockRelayPort";

		#region Properties

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		/// <summary>
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(MockRelayPort); } }

		#endregion

		#region Methods

		/// <summary>
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlFactoryMethod(FACTORY_NAME)]
		public static MockRelayPortSettings FromXml(string xml)
		{
			MockRelayPortSettings output = new MockRelayPortSettings();
			ParseXml(output, xml);
			return output;
		}

		#endregion
	}
}