using System;

namespace ICD.Connect.Protocol.Ports.ComPort
{
	public sealed class ComPortPlusSettings : AbstractComPortSettings
	{
		private const string FACTORY_NAME = "ComPortPlus";

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		/// <summary>
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(ComPortPlus); } }
	}
}
