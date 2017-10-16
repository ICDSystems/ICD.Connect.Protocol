﻿using ICD.Common.Properties;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.Utils;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Tests.Utils
{
	[TestFixture]
	public sealed class ComSpecUtilsTest
	{
		private const ushort OK = 0x4F4B;

		[Test, UsedImplicitly]
		public void LowTest()
		{
			char result = ComSpecUtils.Low(OK);

			Assert.AreEqual(0x4B, result);
			Assert.AreEqual('K', result);
		}

		[Test, UsedImplicitly]
		public void HighTest()
		{
			char result = ComSpecUtils.High(OK);

			Assert.AreEqual(0x4F, result);
			Assert.AreEqual('O', result);
		}

		[Test, UsedImplicitly]
		public void PortCharToIntTest()
		{
			int result = ComSpecUtils.PortCharToInt('A');
			Assert.AreEqual(1, result);

			int goodResult = ComSpecUtils.PortCharToInt('A');
			int badResult = ComSpecUtils.PortCharToInt('a');
			Assert.AreNotEqual(goodResult, badResult);

			result = ComSpecUtils.PortCharToInt('Z');
			Assert.AreEqual(26, result);
		}

		[Test, UsedImplicitly]
		public void AssembleComSpecTest()
		{
			// 1) Port: A; Baud: 9600; Parity: None; Data Bits: 8; Stop Bits: 1; Soft FC: None; Hard FC: None; Protocol: RS-232
			string result = ComSpecUtils.AssembleComSpec('A',
				eComBaudRates.ComspecBaudRate9600,
				eComDataBits.ComspecDataBits8,
				eComParityType.ComspecParityNone,
				eComStopBits.ComspecStopBits1,
				eComProtocolType.ComspecProtocolRS232,
				eComHardwareHandshakeType.ComspecHardwareHandshakeNone,
				eComSoftwareHandshakeType.ComspecSoftwareHandshakeNone,
				true);
			Assert.AreEqual("\x12\xC0\x00\x25\x40\x00\x00", result);

			// 2) Port: B; Baud: 115200; Parity: None; Data Bits: 8; Stop Bits: 1; Soft FC: None; Hard FC: None; Protocol: RS-232
			result = ComSpecUtils.AssembleComSpec('B',
				eComBaudRates.ComspecBaudRate115200,
				eComDataBits.ComspecDataBits8,
				eComParityType.ComspecParityNone,
				eComStopBits.ComspecStopBits1,
				eComProtocolType.ComspecProtocolRS232,
				eComHardwareHandshakeType.ComspecHardwareHandshakeNone,
				eComSoftwareHandshakeType.ComspecSoftwareHandshakeNone,
				true);
			Assert.AreEqual("\x12\xC1\x00\xA6\x40\x00\x00", result);

			// 3) Port: C; Baud: 38400; Parity: Even; Data Bits: 8; Stop Bits: 1; Soft FC: None; Hard FC: None; Protocol: RS-232
			result = ComSpecUtils.AssembleComSpec('C',
				eComBaudRates.ComspecBaudRate38400,
				eComDataBits.ComspecDataBits8,
				eComParityType.ComspecParityEven,
				eComStopBits.ComspecStopBits1,
				eComProtocolType.ComspecProtocolRS232,
				eComHardwareHandshakeType.ComspecHardwareHandshakeNone,
				eComSoftwareHandshakeType.ComspecSoftwareHandshakeNone,
				true);
			Assert.AreEqual("\x12\xC2\x00\x2F\x40\x00\x00", result);

			// 4) Port: D; Baud: 9600; Parity: Odd; Data Bits: 8; Stop Bits: 2; Soft FC: None; Hard FC: None; Protocol: RS-232
			result = ComSpecUtils.AssembleComSpec('D',
				eComBaudRates.ComspecBaudRate9600,
				eComDataBits.ComspecDataBits8,
				eComParityType.ComspecParityOdd,
				eComStopBits.ComspecStopBits2,
				eComProtocolType.ComspecProtocolRS232,
				eComHardwareHandshakeType.ComspecHardwareHandshakeNone,
				eComSoftwareHandshakeType.ComspecSoftwareHandshakeNone,
				true);
			Assert.AreEqual("\x12\xC3\x00\x3D\x40\x00\x00", result);

			// 5) Port: F; Baud: 9600; Parity: Odd; Data Bits: 7; Stop Bits: 1; Soft FC: None; Hard FC: None; Protocol: RS-232
			result = ComSpecUtils.AssembleComSpec('F',
				eComBaudRates.ComspecBaudRate9600,
				eComDataBits.ComspecDataBits7,
				eComParityType.ComspecParityOdd,
				eComStopBits.ComspecStopBits1,
				eComProtocolType.ComspecProtocolRS232,
				eComHardwareHandshakeType.ComspecHardwareHandshakeNone,
				eComSoftwareHandshakeType.ComspecSoftwareHandshakeNone,
				true);
			Assert.AreEqual("\x12\xC5\x00\x1D\x40\x00\x00", result);

			// 6) Port: A; Baud: 19200; Parity: None; Data Bits: 8; Stop Bits: 1; Soft FC: None; Hard FC: None; Protocol: RS-422
			result = ComSpecUtils.AssembleComSpec('A',
				eComBaudRates.ComspecBaudRate19200,
				eComDataBits.ComspecDataBits8,
				eComParityType.ComspecParityNone,
				eComStopBits.ComspecStopBits1,
				eComProtocolType.ComspecProtocolRS422,
				eComHardwareHandshakeType.ComspecHardwareHandshakeNone,
				eComSoftwareHandshakeType.ComspecSoftwareHandshakeNone,
				true);
			Assert.AreEqual("\x12\xC0\x00\x26\x41\x00\x00", result);

			// 7) Port: B; Baud: 9600; Parity: None; Data Bits: 8; Stop Bits: 1; Soft FC: XON/XOFF; Hard FC: None; Protocol: RS-232
			result = ComSpecUtils.AssembleComSpec('B',
				eComBaudRates.ComspecBaudRate9600,
				eComDataBits.ComspecDataBits8,
				eComParityType.ComspecParityNone,
				eComStopBits.ComspecStopBits1,
				eComProtocolType.ComspecProtocolRS232,
				eComHardwareHandshakeType.ComspecHardwareHandshakeNone,
				eComSoftwareHandshakeType.ComspecSoftwareHandshakeXON,
				true);
			Assert.AreEqual("\x12\xC1\x00\x25\x58\x00\x00", result);

			// 8) Port: F; Baud: 57600; Parity: None; Data Bits: 8; Stop Bits: 1; Soft FC: None; Hard FC: RTS/CTS; Protocol: RS-232
			result = ComSpecUtils.AssembleComSpec('F',
				eComBaudRates.ComspecBaudRate57600,
				eComDataBits.ComspecDataBits8,
				eComParityType.ComspecParityNone,
				eComStopBits.ComspecStopBits1,
				eComProtocolType.ComspecProtocolRS232,
				eComHardwareHandshakeType.ComspecHardwareHandshakeRTSCTS,
				eComSoftwareHandshakeType.ComspecSoftwareHandshakeNone,
				true);
			Assert.AreEqual("\x12\xC5\x00\xA5\x46\x00\x00", result);

			// 9) Port: E; Baud: 38400; Parity: Zero Stick; Data Bits: 8; Stop Bits: 1; Soft FC: None; Hard FC: RTC/CTS; Protocol: RS-232
			result = ComSpecUtils.AssembleComSpec('E',
				eComBaudRates.ComspecBaudRate38400,
				eComDataBits.ComspecDataBits8,
				eComParityType.ComspecParityZeroStick,
				eComStopBits.ComspecStopBits1,
				eComProtocolType.ComspecProtocolRS232,
				eComHardwareHandshakeType.ComspecHardwareHandshakeRTSCTS,
				eComSoftwareHandshakeType.ComspecSoftwareHandshakeNone,
				true);
			Assert.AreEqual("\x12\xC4\x00\x37\x46\x00\x00", result);

			// 10) Port: F; Baud: 9600; Parity: None; Data Bits: 8; Stop Bits: 1; Soft FC: None; Hard FC: None; Protocol: RS-485
			result = ComSpecUtils.AssembleComSpec('F',
				eComBaudRates.ComspecBaudRate9600,
				eComDataBits.ComspecDataBits8,
				eComParityType.ComspecParityNone,
				eComStopBits.ComspecStopBits1,
				eComProtocolType.ComspecProtocolRS485,
				eComHardwareHandshakeType.ComspecHardwareHandshakeNone,
				eComSoftwareHandshakeType.ComspecSoftwareHandshakeNone,
				true);
			Assert.AreEqual("\x12\xC5\x00\x25\x61\x00\x00", result);
		}
	}
}
