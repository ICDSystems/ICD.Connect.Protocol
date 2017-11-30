using System;
using ICD.Common.Properties;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.Mock.Ports.ComPort
{
	public sealed class MockComPortSettings : AbstractComPortSettings
	{
		private const string FACTORY_NAME = "MockComPort";

		#region Properties

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		/// <summary>
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(MockComPort); } }

		#endregion

		#region Methods

		/// <summary>
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlFactoryMethod(FACTORY_NAME)]
		public static MockComPortSettings FromXml(string xml)
		{
			MockComPortSettings output = new MockComPortSettings();
			ParseXml(output, xml);
			return output;
		}

		#endregion
	}
}