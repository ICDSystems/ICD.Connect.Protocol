using ICD.Common.Properties;
using ICD.Connect.Protocol.Settings;

namespace ICD.Connect.Protocol.Ports.ComPort
{
	/// <summary>
	/// IComPort removes a dependency on the Pro library ComPort.
	/// </summary>
	public interface IComPort : ISerialPort
	{
		/// <summary>
		/// Gets the Com Spec configuration properties.
		/// </summary>
		[NotNull]
		IComSpecProperties ComSpecProperties { get; }

		/// <summary>
		/// Gets the baud rate.
		/// </summary>
		eComBaudRates BaudRate { get; }

		/// <summary>
		/// Gets the number of data bits.
		/// </summary>
		eComDataBits NumberOfDataBits { get; }

		/// <summary>
		/// Gets the parity type.
		/// </summary>
		eComParityType ParityType { get; }

		/// <summary>
		/// Gets the number of stop bits.
		/// </summary>
		eComStopBits NumberOfStopBits { get; }

		/// <summary>
		/// Gets the protocol type.
		/// </summary>
		eComProtocolType ProtocolType { get; }

		/// <summary>
		/// Gets the hardware handshake mode.
		/// </summary>
		eComHardwareHandshakeType HardwareHandShake { get; }

		/// <summary>
		/// Gets the software handshake mode.
		/// </summary>
		eComSoftwareHandshakeType SoftwareHandshake { get; }

		/// <summary>
		/// Gets the report CTS changes mode.
		/// </summary>
		bool ReportCtsChanges { get; }

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
