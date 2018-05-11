using ICD.Connect.Settings;

namespace ICD.Connect.Protocol.Settings
{
	public interface IIrDriverSettings : ISettings
	{
		/// <summary>
		/// Gets/sets the configurable path to the IR driver.
		/// </summary>
		string IrDriverPath { get; set; }

		/// <summary>
		/// Gets/sets the configurable pulse time for the IR driver.
		/// </summary>
		ushort IrPulseTime { get; set; }

		/// <summary>
		/// Gets/sets the configurable between time for the IR driver.
		/// </summary>
		ushort IrBetweenTime { get; set; }
	}
}
