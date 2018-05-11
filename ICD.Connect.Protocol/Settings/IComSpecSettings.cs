using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Settings;

namespace ICD.Connect.Protocol.Settings
{
	public interface IComSpecSettings : ISettings
	{
		/// <summary>
		/// Gets/sets the configurable baud rate.
		/// </summary>
		eComBaudRates ComSpecBaudRate { get; set; }

		/// <summary>
		/// Gets/sets the configurable number of data bits.
		/// </summary>
		eComDataBits ComSpecNumberOfDataBits { get; set; }

		/// <summary>
		/// Gets/sets the configurable parity type.
		/// </summary>
		eComParityType ComSpecParityType { get; set; }

		/// <summary>
		/// Gets/sets the configurable number of stop bits.
		/// </summary>
		eComStopBits ComSpecNumberOfStopBits { get; set; }

		/// <summary>
		/// Gets/sets the configurable protocol type.
		/// </summary>
		eComProtocolType ComSpecProtocolType { get; set; }

		/// <summary>
		/// Gets/sets the configurable hardware handshake type.
		/// </summary>
		eComHardwareHandshakeType ComSpecHardwareHandShake { get; set; }

		/// <summary>
		/// Gets/sets the configurable software handshake type.
		/// </summary>
		eComSoftwareHandshakeType ComSpecSoftwareHandshake { get; set; }

		/// <summary>
		/// Gets/sets the configurable report CTS changes state.
		/// </summary>
		bool ComSpecReportCtsChanges { get; set; }
	}
}
