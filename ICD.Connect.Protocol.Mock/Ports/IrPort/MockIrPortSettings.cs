using System;
using ICD.Common.Properties;
using ICD.Connect.Protocol.Ports.IrPort;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.Mock.Ports.IrPort
{
	public sealed class MockIrPortSettings : AbstractIrPortSettings
	{
		private const string FACTORY_NAME = "MockIrPort";

		#region Properties

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		/// <summary>
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(MockIrPort); } }

		#endregion

		#region Methods

		/// <summary>
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlFactoryMethod(FACTORY_NAME)]
		public static MockIrPortSettings FromXml(string xml)
		{
			MockIrPortSettings output = new MockIrPortSettings();
			ParseXml(output, xml);
			return output;
		}

		#endregion
	}
}