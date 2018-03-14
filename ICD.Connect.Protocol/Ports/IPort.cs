using ICD.Common.Properties;
using ICD.Connect.Devices;

namespace ICD.Connect.Protocol.Ports
{
	/// <summary>
	/// Base interface for all ports.
	/// </summary>
	public interface IPort : IDeviceBase
	{
		/// <summary>
		/// Sets the type of debug message to print for received data to the console.
		/// </summary>
		[PublicAPI]
		eDebugMode DebugRx { get; set; }

		/// <summary>
		/// Sets the type of debug message  to print for transmitted data to the console.
		/// </summary>
		[PublicAPI]
		eDebugMode DebugTx { get; set; }
	}
}
