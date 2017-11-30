using System;
using ICD.Common.Properties;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.Mock.Ports
{
	public sealed class MockSerialPortSettings : AbstractSerialPortSettings
	{
		private const string FACTORY_NAME = "MockSerialPort";

		#region Properties

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		/// <summary>
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(MockSerialPort); } }

		#endregion

		#region Methods

		/// <summary>
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlFactoryMethod(FACTORY_NAME)]
		public static MockSerialPortSettings FromXml(string xml)
		{
			MockSerialPortSettings output = new MockSerialPortSettings();
			ParseXml(output, xml);
			return output;
		}

		#endregion
	}
}