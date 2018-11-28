using ICD.Connect.Protocol.Settings;

namespace ICD.Connect.Protocol.Ports.ComPort
{
	/// <summary>
	/// IComPort removes a dependency on the Pro library ComPort.
	/// </summary>
	public interface IComPort : ISerialPort
	{
		/// <summary>
		/// Configures the ComPort for communication.
		/// </summary>
		/// <param name="comSpec"></param>
		void SetComPortSpec(ComSpec comSpec);

		/// <summary>
		/// Applies the given device configuration properties to the port.
		/// </summary>
		/// <param name="properties"></param>
		void ApplyDeviceConfiguration(IComSpecProperties properties);

		/// <summary>
		/// Applies the given configuration properties to the port.
		/// </summary>
		/// <param name="properties"></param>
		void ApplyConfiguration(IComSpecProperties properties);
	}
}
