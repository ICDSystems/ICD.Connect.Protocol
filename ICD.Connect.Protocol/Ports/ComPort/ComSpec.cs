using System;

namespace ICD.Connect.Protocol.Ports.ComPort
{
	#region ComPort enums

	[Flags]
	public enum eComBaudRates
	{
		ComspecBaudRate300 = 1,
		ComspecBaudRate600 = 2,
		ComspecBaudRate1200 = 4,
		ComspecBaudRate1800 = 8,
		ComspecBaudRate2400 = 16,
		ComspecBaudRate3600 = 32,
		ComspecBaudRate4800 = 64,
		ComspecBaudRate7200 = 128,
		ComspecBaudRate9600 = 256,
		ComspecBaudRate14400 = 512,
		ComspecBaudRate19200 = 1024,
		ComspecBaudRate28800 = 2048,
		ComspecBaudRate38400 = 4096,
		ComspecBaudRate57600 = 8192,
		ComspecBaudRate115200 = 65536,
	}

	public enum eComDataBits
	{
		ComspecDataBits7 = 7,
		ComspecDataBits8 = 8,
	}

	public enum eComHardwareHandshakeType
	{
		// ReSharper disable InconsistentNaming
		ComspecHardwareHandshakeNone = 0,
		ComspecHardwareHandshakeRTS = 1,
		ComspecHardwareHandshakeCTS = 2,
		ComspecHardwareHandshakeRTSCTS = 3,
		// ReSharper restore InconsistentNaming
	}

	public enum eComParityType
	{
		ComspecParityNone = 0,
		ComspecParityEven = 1,
		ComspecParityOdd = 2,
		ComspecParityZeroStick = 3
	}

	public enum eComProtocolType
	{
		// ReSharper disable InconsistentNaming
		ComspecProtocolRS232 = 0,
		ComspecProtocolRS422 = 1,
		ComspecProtocolRS485 = 2,
		// ReSharper restore InconsistentNaming
	}

	public enum eComSoftwareHandshakeType
	{
		// ReSharper disable InconsistentNaming
		ComspecSoftwareHandshakeNone = 0,
		ComspecSoftwareHandshakeXON = 1,
		ComspecSoftwareHandshakeXONT = 2,
		ComspecSoftwareHandshakeXONR = 3,
		// ReSharper restore InconsistentNaming
	}

	public enum eComStopBits
	{
		ComspecStopBits1 = 1,
		ComspecStopBits2 = 2,
	}

	#endregion

	public sealed class ComSpec
	{
		public eComBaudRates BaudRate { get; set; }
		public eComDataBits NumberOfDataBits { get; set; }
		public eComParityType ParityType { get; set; }
		public eComStopBits NumberOfStopBits { get; set; }
		public eComProtocolType ProtocolType { get; set; }
		public eComHardwareHandshakeType HardwareHandShake { get; set; }
		public eComSoftwareHandshakeType SoftwareHandshake { get; set; }
		public bool ReportCtsChanges { get; set; }
	}
}
