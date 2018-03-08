using System;
using ICD.Common.Properties;
using ICD.Connect.Protocol.Ports.IoPort;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.Mock.Ports.IoPort
{
	[KrangSettings(FACTORY_NAME)]
	public sealed class MockIoPortSettings : AbstractIoPortSettings
	{
		private const string FACTORY_NAME = "MockIoPort";

		#region Properties

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		/// <summary>
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(MockIoPort); } }

		#endregion
	}
}