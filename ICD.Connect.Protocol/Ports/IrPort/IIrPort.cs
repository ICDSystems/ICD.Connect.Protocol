using ICD.Common.Properties;

namespace ICD.Connect.Protocol.Ports.IrPort
{
	/// <summary>
	/// Interface for managing an IR Port.
	/// </summary>
	public interface IIrPort : IPort
	{
		#region Properties

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

		#endregion
	}
}
