using System;
using System.Collections.Generic;

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

		/// <summary>
		/// Returns the collection of ids that the settings will depend on.
		/// For example, to instantiate an IR Port from settings, the device the physical port
		/// belongs to will need to be instantiated first.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<int> GetDeviceDependencies()
		{
			yield break;
		}
	}
}
