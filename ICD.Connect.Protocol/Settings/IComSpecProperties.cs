﻿using System;
using ICD.Connect.Protocol.Ports.ComPort;

namespace ICD.Connect.Protocol.Settings
{
	public interface IComSpecProperties
	{
		/// <summary>
		/// Gets/sets the configurable baud rate.
		/// </summary>
		eComBaudRates? ComSpecBaudRate { get; set; }

		/// <summary>
		/// Gets/sets the configurable number of data bits.
		/// </summary>
		eComDataBits? ComSpecNumberOfDataBits { get; set; }

		/// <summary>
		/// Gets/sets the configurable parity type.
		/// </summary>
		eComParityType? ComSpecParityType { get; set; }

		/// <summary>
		/// Gets/sets the configurable number of stop bits.
		/// </summary>
		eComStopBits? ComSpecNumberOfStopBits { get; set; }

		/// <summary>
		/// Gets/sets the configurable protocol type.
		/// </summary>
		eComProtocolType? ComSpecProtocolType { get; set; }

		/// <summary>
		/// Gets/sets the configurable hardware handshake type.
		/// </summary>
		eComHardwareHandshakeType? ComSpecHardwareHandshake { get; set; }

		/// <summary>
		/// Gets/sets the configurable software handshake type.
		/// </summary>
		eComSoftwareHandshakeType? ComSpecSoftwareHandshake { get; set; }

		/// <summary>
		/// Gets/sets the configurable report CTS changes state.
		/// </summary>
		bool? ComSpecReportCtsChanges { get; set; }

		/// <summary>
		/// Clears the configured values.
		/// </summary>
		void ClearComSpecProperties();
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
			extends.ComSpecHardwareHandshake = other.ComSpecHardwareHandshake;
			extends.ComSpecSoftwareHandshake = other.ComSpecSoftwareHandshake;
			extends.ComSpecReportCtsChanges = other.ComSpecReportCtsChanges;
		}

        /// <summary>
        /// Copies the configured properties from the given ComSpec instance.
        /// </summary>
        /// <param name="extends"></param>
        /// <param name="other"></param>
        public static void Copy(this IComSpecProperties extends, ComSpec other)
        {
            if (extends == null)
                throw new ArgumentNullException("extends");

            if (other == null)
                throw new ArgumentNullException("other");

            extends.ComSpecBaudRate = other.BaudRate;
            extends.ComSpecNumberOfDataBits = other.NumberOfDataBits;
            extends.ComSpecParityType = other.ParityType;
            extends.ComSpecNumberOfStopBits = other.NumberOfStopBits;
            extends.ComSpecProtocolType = other.ProtocolType;
            extends.ComSpecHardwareHandshake = other.HardwareHandshake;
            extends.ComSpecSoftwareHandshake = other.SoftwareHandshake;
            extends.ComSpecReportCtsChanges = other.ReportCtsChanges;
        }

		/// <summary>
		/// Updates the ComSpec Properties instance where values are not already configured.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="baudRate"></param>
		/// <param name="numberOfDataBits"></param>
		/// <param name="parityType"></param>
		/// <param name="numberOfStopBits"></param>
		/// <param name="protocolType"></param>
		/// <param name="hardwareHandShake"></param>
		/// <param name="softwareHandshake"></param>
		/// <param name="reportCtsChanges"></param>
		public static void ApplyDefaultValues(this IComSpecProperties extends, eComBaudRates? baudRate,
		                                      eComDataBits? numberOfDataBits, eComParityType? parityType,
		                                      eComStopBits? numberOfStopBits, eComProtocolType? protocolType,
		                                      eComHardwareHandshakeType? hardwareHandShake,
		                                      eComSoftwareHandshakeType? softwareHandshake, bool? reportCtsChanges)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (extends.ComSpecBaudRate == null)
				extends.ComSpecBaudRate = baudRate;

			if (extends.ComSpecNumberOfDataBits == null)
				extends.ComSpecNumberOfDataBits = numberOfDataBits;

			if (extends.ComSpecParityType == null)
				extends.ComSpecParityType = parityType;

			if (extends.ComSpecNumberOfStopBits == null)
				extends.ComSpecNumberOfStopBits = numberOfStopBits;

			if (extends.ComSpecProtocolType == null)
				extends.ComSpecProtocolType = protocolType;

			if (extends.ComSpecHardwareHandshake == null)
				extends.ComSpecHardwareHandshake = hardwareHandShake;

			if (extends.ComSpecSoftwareHandshake == null)
				extends.ComSpecSoftwareHandshake = softwareHandshake;

			if (extends.ComSpecReportCtsChanges == null)
				extends.ComSpecReportCtsChanges = reportCtsChanges;
		}

		/// <summary>
		/// Creates a new properties instance, applying this instance over the top of the other instance.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="other"></param>
		/// <returns></returns>
		public static IComSpecProperties Superimpose(this IComSpecProperties extends, IComSpecProperties other)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (other == null)
				throw new ArgumentNullException("other");

			ComSpecProperties output = new ComSpecProperties();

			output.Copy(extends);
			output.ApplyDefaultValues(other.ComSpecBaudRate,
									  other.ComSpecNumberOfDataBits,
									  other.ComSpecParityType,
									  other.ComSpecNumberOfStopBits,
									  other.ComSpecProtocolType,
									  other.ComSpecHardwareHandshake,
									  other.ComSpecSoftwareHandshake,
									  other.ComSpecReportCtsChanges);

			return output;
		}
	}
}
