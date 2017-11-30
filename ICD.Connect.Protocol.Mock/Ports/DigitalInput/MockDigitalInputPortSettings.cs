using System;
using ICD.Common.Properties;
using ICD.Connect.Protocol.Ports.DigitalInput;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.Mock.Ports.DigitalInput
{
	public sealed class MockDigitalInputPortSettings : AbstractDigitalInputPortSettings
	{
		private const string FACTORY_NAME = "MockDigitalInputPort";

		#region Properties

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		/// <summary>
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(MockDigitalInputPort); } }

		#endregion

		#region Methods

		/// <summary>
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlFactoryMethod(FACTORY_NAME)]
		public static MockDigitalInputPortSettings FromXml(string xml)
		{
			MockDigitalInputPortSettings output = new MockDigitalInputPortSettings();
			ParseXml(output, xml);
			return output;
		}

		#endregion
	}
}