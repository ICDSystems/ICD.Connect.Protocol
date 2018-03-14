using System;
using ICD.Connect.Protocol.Ports.IrPort;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.Mock.Ports.IrPort
{
	[KrangSettings(FACTORY_NAME)]
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
	}
}