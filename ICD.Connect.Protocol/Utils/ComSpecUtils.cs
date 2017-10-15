using System;
using ICD.Common.Properties;
using ICD.Connect.Protocol.Ports.ComPort;

namespace ICD.Connect.Protocol.Utils
{
	public static class ComSpecUtils
	{
		/// <summary>
		/// Returns the number of stop bits.
		/// </summary>
		/// <param name="stopBits"></param>
		/// <returns></returns>
		[PublicAPI]
		public static int StopBitsToCount(eComStopBits stopBits)
		{
			switch (stopBits)
			{
				case eComStopBits.ComspecStopBits1:
					return 1;
				case eComStopBits.ComspecStopBits2:
					return 2;
				default:
					throw new ArgumentOutOfRangeException("stopBits");
			}
		}

		/// <summary>
		/// Returns the number of stop bits.
		/// </summary>
		/// <param name="count"></param>
		/// <returns></returns>
		[PublicAPI]
		public static eComStopBits StopBitsFromCount(int count)
		{
			switch (count)
			{
				case 1:
					return eComStopBits.ComspecStopBits1;
				case 2:
					return eComStopBits.ComspecStopBits2;

				default:
					throw new ArgumentOutOfRangeException("count");
			}
		}

		/// <summary>
		/// Returns the numeric portion of the baud rate.
		/// </summary>
		/// <param name="baudRate"></param>
		/// <returns></returns>
		[PublicAPI]
		public static int BaudRateToRate(eComBaudRates baudRate)
		{
			switch (baudRate)
			{
				case eComBaudRates.ComspecBaudRate300:
					return 300;
				case eComBaudRates.ComspecBaudRate600:
					return 600;
				case eComBaudRates.ComspecBaudRate1200:
					return 1200;
				case eComBaudRates.ComspecBaudRate1800:
					return 1800;
				case eComBaudRates.ComspecBaudRate2400:
					return 2400;
				case eComBaudRates.ComspecBaudRate3600:
					return 3600;
				case eComBaudRates.ComspecBaudRate4800:
					return 4800;
				case eComBaudRates.ComspecBaudRate7200:
					return 7200;
				case eComBaudRates.ComspecBaudRate9600:
					return 9600;
				case eComBaudRates.ComspecBaudRate14400:
					return 14400;
				case eComBaudRates.ComspecBaudRate19200:
					return 19200;
				case eComBaudRates.ComspecBaudRate28800:
					return 28800;
				case eComBaudRates.ComspecBaudRate38400:
					return 38400;
				case eComBaudRates.ComspecBaudRate57600:
					return 57600;
				case eComBaudRates.ComspecBaudRate115200:
					return 115200;

				default:
					throw new ArgumentOutOfRangeException("baudRate");
			}
		}

		/// <summary>
		/// Returns the baud rate for the given numeric value.
		/// </summary>
		/// <param name="rate"></param>
		/// <returns></returns>
		[PublicAPI]
		public static eComBaudRates BaudRateFromRate(int rate)
		{
			switch (rate)
			{
				case 300:
					return eComBaudRates.ComspecBaudRate300;
				case 600:
					return eComBaudRates.ComspecBaudRate600;
				case 1200:
					return eComBaudRates.ComspecBaudRate1200;
				case 1800:
					return eComBaudRates.ComspecBaudRate1800;
				case 2400:
					return eComBaudRates.ComspecBaudRate2400;
				case 3600:
					return eComBaudRates.ComspecBaudRate3600;
				case 4800:
					return eComBaudRates.ComspecBaudRate4800;
				case 7200:
					return eComBaudRates.ComspecBaudRate7200;
				case 9600:
					return eComBaudRates.ComspecBaudRate9600;
				case 14400:
					return eComBaudRates.ComspecBaudRate14400;
				case 19200:
					return eComBaudRates.ComspecBaudRate19200;
				case 28800:
					return eComBaudRates.ComspecBaudRate28800;
				case 38400:
					return eComBaudRates.ComspecBaudRate38400;
				case 57600:
					return eComBaudRates.ComspecBaudRate57600;
				case 115200:
					return eComBaudRates.ComspecBaudRate115200;

				default:
					throw new ArgumentOutOfRangeException("rate");
			}
		}

		[PublicAPI("S+")]
		public static ushort SpecBaudRate(eComBaudRates baudRate)
		{
			switch (baudRate)
			{
				case eComBaudRates.ComspecBaudRate300:
					return 0x0000;
				case eComBaudRates.ComspecBaudRate600:
					return 0x0001;
				case eComBaudRates.ComspecBaudRate1200:
					return 0x0002;
				case eComBaudRates.ComspecBaudRate1800:
					return 0x0080;
				case eComBaudRates.ComspecBaudRate2400:
					return 0x0003;
				case eComBaudRates.ComspecBaudRate3600:
					return 0x0081;
				case eComBaudRates.ComspecBaudRate4800:
					return 0x0004;
				case eComBaudRates.ComspecBaudRate7200:
					return 0x0082;
				case eComBaudRates.ComspecBaudRate9600:
					return 0x0005;
				case eComBaudRates.ComspecBaudRate14400:
					return 0x0083;
				case eComBaudRates.ComspecBaudRate19200:
					return 0x0006;
				case eComBaudRates.ComspecBaudRate28800:
					return 0x0084;
				case eComBaudRates.ComspecBaudRate38400:
					return 0x0007;
				case eComBaudRates.ComspecBaudRate57600:
					return 0x0085;
				case eComBaudRates.ComspecBaudRate115200:
					return 0x0086;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		[PublicAPI("S+")]
		public static ushort SpecProto(eComProtocolType protocol)
		{
			switch (protocol)
			{
				case eComProtocolType.ComspecProtocolRS232:
					return 0x0000;
				case eComProtocolType.ComspecProtocolRS422:
					return 0x0100;
				case eComProtocolType.ComspecProtocolRS485:
					return 0x2100;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		[PublicAPI("S+")]
		public static ushort SpecParity(eComParityType parity)
		{
			switch (parity)
			{
				case eComParityType.ComspecParityNone:
					return 0x0000;
				case eComParityType.ComspecParityZeroStick:
					return 0x0010;
				case eComParityType.ComspecParityOdd:
					return 0x0018;
				case eComParityType.ComspecParityEven:
					return 0x0008;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		[PublicAPI("S+")]
		public static ushort SpecDataBits(eComDataBits dataBits)
		{
			switch (dataBits)
			{
				case eComDataBits.ComspecDataBits7:
					return 0x0000;
				case eComDataBits.ComspecDataBits8:
					return 0x0020;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		[PublicAPI("S+")]
		public static ushort SpecStopBits(eComStopBits stopBits)
		{
			switch (stopBits)
			{
				case eComStopBits.ComspecStopBits1:
					return 0x0000;
				case eComStopBits.ComspecStopBits2:
					return 0x0040;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		[PublicAPI("S+")]
		public static ushort SpecSoftFc(eComSoftwareHandshakeType softFc)
		{
			switch (softFc)
			{
				case eComSoftwareHandshakeType.ComspecSoftwareHandshakeNone:
					return 0x0000;
				case eComSoftwareHandshakeType.ComspecSoftwareHandshakeXON:
					return 0x1800;
				case eComSoftwareHandshakeType.ComspecSoftwareHandshakeXONT:
					return 0x0800;
				case eComSoftwareHandshakeType.ComspecSoftwareHandshakeXONR:
					return 0x1000;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		[PublicAPI("S+")]
		public static ushort SpecHardFc(eComHardwareHandshakeType hardFc)
		{
			switch (hardFc)
			{
				case eComHardwareHandshakeType.ComspecHardwareHandshakeNone:
					return 0x0000;
				case eComHardwareHandshakeType.ComspecHardwareHandshakeRTS:
					return 0x0400;
				case eComHardwareHandshakeType.ComspecHardwareHandshakeCTS:
					return 0x0200;
				case eComHardwareHandshakeType.ComspecHardwareHandshakeRTSCTS:
					return (0x0400 | 0x0200);

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		[PublicAPI("S+")]
		public static ushort SpecReportCts(bool reportCts)
		{
			return (ushort)(reportCts ? 0x4000 : 0x0000);
		}

		/// <summary>
		///		Returns the lowest 8 bits of the 16 bit integer.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		[PublicAPI("S+")]
		public static char Low(ushort input)
		{
			return (char)(input & 0xFF);
		}

		/// <summary>
		///		Returns the highest 8 bits of the 16 bit integer.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		[PublicAPI("S+")]
		public static char High(ushort input)
		{
			return (char)(input >> 8);
		}

		/// <summary>
		/// Converts the port character to an integer (A=1, B=2, etc)
		/// </summary>
		/// <param name="port"></param>
		/// <returns></returns>
		[PublicAPI("S+")]
		public static int PortCharToInt(char port)
		{
			// ASCII 'A' is decimal 65.
			return port - 64;
		}

		[PublicAPI("S+")]
		public static string AssembleComSpec(char port, eComBaudRates baudRate, eComDataBits numberOfDataBits,
		                                     eComParityType parityType,
		                                     eComStopBits numberOfStopBits, eComProtocolType protocolType,
		                                     eComHardwareHandshakeType hardwareHandShake,
		                                     eComSoftwareHandshakeType softwareHandshake, bool reportCtsChanges)
		{
			int portInt = PortCharToInt(port);

			return AssembleComSpec(portInt, baudRate, numberOfDataBits, parityType, numberOfStopBits,
			                       protocolType, hardwareHandShake, softwareHandshake, reportCtsChanges);
		}

		[PublicAPI("S+")]
		public static string AssembleComSpec(int port, eComBaudRates baudRate, eComDataBits numberOfDataBits,
		                                     eComParityType parityType,
		                                     eComStopBits numberOfStopBits, eComProtocolType protocolType,
		                                     eComHardwareHandshakeType hardwareHandShake,
		                                     eComSoftwareHandshakeType softwareHandshake, bool reportCtsChanges)
		{
			// ComSpec Port 1/'A' is 64.
			port += 63;

			ushort cspec = SpecBaudRate(baudRate);
			cspec |= SpecProto(protocolType);
			cspec |= SpecParity(parityType);
			cspec |= SpecDataBits(numberOfDataBits);
			cspec |= SpecSoftFc(softwareHandshake);
			cspec |= SpecHardFc(hardwareHandShake);
			cspec |= SpecReportCts(reportCtsChanges);

			return string.Format("{0}{1}{2}{3}{4}{5}{6}", (char)0x12, (char)(0x80 | port), (char)0x00,
			                     Low(cspec), High(cspec), (char)0x00, (char)0x00);
		}
	}
}
