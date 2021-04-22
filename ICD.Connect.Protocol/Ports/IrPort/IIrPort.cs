using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Connect.Protocol.Ports.IrPort.IrPulse;
using ICD.Connect.Protocol.Settings;

namespace ICD.Connect.Protocol.Ports.IrPort
{
	/// <summary>
	/// Interface for managing an IR Port.
	/// </summary>
	public interface IIrPort : IPort
	{
		#region Properties

		/// <summary>
		/// Controls pulsing and timing for the IR port.
		/// </summary>
		IrPortPulseComponent PulseComponent { get; set; }

		/// <summary>
		/// Gets the IR Driver configuration.
		/// </summary>
		IIrDriverProperties IrDriverProperties { get; }

		/// <summary>
		/// Gets the path to the loaded IR driver.
		/// </summary>
		string DriverPath { get; }

		/// <summary>
		/// Gets/sets the default pulse time in milliseconds for a PressAndRelease.
		/// </summary>
		[PublicAPI]
		ushort PulseTime { get; set; }

		/// <summary>
		/// Gets/sets the default time in milliseconds between PressAndRelease commands.
		/// </summary>
		[PublicAPI]
		ushort BetweenTime { get; set; }

		#endregion

		#region Methods

		/// <summary>
		/// Loads the driver from the given path.
		/// </summary>
		/// <param name="path"></param>
		[PublicAPI]
		void LoadDriver(string path);

		/// <summary>
		/// Gets the loaded IR commands.
		/// </summary>
		/// <returns></returns>
		IEnumerable<string> GetCommands();

		/// <summary>
		/// Begin sending the command.
		/// </summary>
		/// <param name="command"></param>
		[PublicAPI]
		void Press(string command);

		/// <summary>
		/// Stop sending the current command.
		/// </summary>
		[PublicAPI]
		void Release();

		/// <summary>
		/// Sends the command for the default pulse time.
		/// </summary>
		/// <param name="command"></param>
		[PublicAPI]
		void PressAndRelease(string command);

		/// <summary>
		/// Send the command for the given number of milliseconds.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="pulseTime"></param>
		[PublicAPI]
		void PressAndRelease(string command, ushort pulseTime);

		/// <summary>
		/// Sends the command for the given number of milliseconds.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="pulseTime"></param>
		/// <param name="betweenTime"></param>
		[PublicAPI]
		void PressAndRelease(string command, ushort pulseTime, ushort betweenTime);

		/// <summary>
		/// Applies the given device configuration properties to the port.
		/// </summary>
		/// <param name="properties"></param>
		void ApplyDeviceConfiguration(IIrDriverProperties properties);

		/// <summary>
		/// Applies the given configuration properties to the port.
		/// </summary>
		/// <param name="properties"></param>
		void ApplyConfiguration(IIrDriverProperties properties);

		#endregion
	}
}
