using System;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Protocol.Settings
{
	public interface IIrDriverProperties
	{
		/// <summary>
		/// Gets/sets the configurable path to the IR driver.
		/// </summary>
		[PathSettingsProperty("IRDrivers", ".ir")]
		string IrDriverPath { get; set; }

		/// <summary>
		/// Gets/sets the configurable pulse time for the IR driver.
		/// </summary>
		ushort? IrPulseTime { get; set; }

		/// <summary>
		/// Gets/sets the configurable between time for the IR driver.
		/// </summary>
		ushort? IrBetweenTime { get; set; }
	}

	public static class IrDriverPropertiesExtensions
	{
		/// <summary>
		/// Copies the configured properties from the given IR Properties instance.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="other"></param>
		public static void Copy(this IIrDriverProperties extends, IIrDriverProperties other)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (other == null)
				throw new ArgumentNullException("other");

			extends.IrDriverPath = other.IrDriverPath;
			extends.IrPulseTime = other.IrPulseTime;
			extends.IrBetweenTime = other.IrBetweenTime;
		}

		/// <summary>
		/// Updates the IR Driver Properties instance where values are not already configured.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="driverPath"></param>
		/// <param name="pulseTime"></param>
		/// <param name="betweenTime"></param>
		public static void ApplyDefaultValues(this IIrDriverProperties extends, string driverPath, ushort? pulseTime, ushort? betweenTime)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (extends.IrDriverPath == null)
				extends.IrDriverPath = driverPath;

			if (extends.IrPulseTime == null)
				extends.IrPulseTime = pulseTime;

			if (extends.IrBetweenTime == null)
				extends.IrBetweenTime = betweenTime;
		}
	}
}
