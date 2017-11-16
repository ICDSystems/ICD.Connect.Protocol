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
		/// When enabled prints the received data to the console.
		/// </summary>
		[PublicAPI]
		bool DebugRx { get; set; }

		/// <summary>
		/// When enabled prints the transmitted data to the console.
		/// </summary>
		[PublicAPI]
		bool DebugTx { get; set; }
	}
}
