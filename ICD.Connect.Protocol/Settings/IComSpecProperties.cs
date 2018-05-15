using System;
using ICD.Connect.Protocol.Ports.ComPort;

namespace ICD.Connect.Protocol.Settings
{
	public interface IComSpecProperties
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

	public static class ComSpecPropertiesExtensions
	{
		/// <summary>
		/// Copies the configured properties from the given ComSpec Properties instance.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="other"></param>
		public static void Copy(this IComSpecProperties extends, IComSpecProperties other)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (other == null)
				throw new ArgumentNullException("other");

			extends.ComSpecBaudRate = other.ComSpecBaudRate;
			extends.ComSpecNumberOfDataBits = other.ComSpecNumberOfDataBits;
			extends.ComSpecParityType = other.ComSpecParityType;
			extends.ComSpecNumberOfStopBits = other.ComSpecNumberOfStopBits;
			extends.ComSpecProtocolType = other.ComSpecProtocolType;
			extends.ComSpecHardwareHandShake = other.ComSpecHardwareHandShake;
			extends.ComSpecSoftwareHandshake = other.ComSpecSoftwareHandshake;
			extends.ComSpecReportCtsChanges = other.ComSpecReportCtsChanges;
		}
	}
}
