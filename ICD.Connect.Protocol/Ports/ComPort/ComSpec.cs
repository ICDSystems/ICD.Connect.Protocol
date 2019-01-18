using System;

namespace ICD.Connect.Protocol.Ports.ComPort
{
	#region ComPort enums

	[Flags]
	public enum eComBaudRates
	{
		BaudRate300 = 1,
		BaudRate600 = 2,
		BaudRate1200 = 4,
		BaudRate1800 = 8,
		BaudRate2400 = 16,
		BaudRate3600 = 32,
		BaudRate4800 = 64,
		BaudRate7200 = 128,
		BaudRate9600 = 256,
		BaudRate14400 = 512,
		BaudRate19200 = 1024,
		BaudRate28800 = 2048,
		BaudRate38400 = 4096,
		BaudRate57600 = 8192,
		BaudRate115200 = 65536,
	}

	public enum eComDataBits
	{
		DataBits7 = 7,
		DataBits8 = 8,
	}

	public enum eComHardwareHandshakeType
	{
		None = 0,
		Rts = 1,
		Cts = 2,
		RtsCts = 3,
	}

	public enum eComParityType
	{
		None = 0,
		Even = 1,
		Odd = 2,
		Mark = 3
	}

	public enum eComProtocolType
	{
		Rs232 = 0,
		Rs422 = 1,
		Rs485 = 2,
	}

	public enum eComSoftwareHandshakeType
	{
		None = 0,
		XOn = 1,
		XOnTransmit = 2,
		XOnReceive = 3,
	}

	public enum eComStopBits
	{
		StopBits1 = 1,
		StopBits2 = 2,
	}

	#endregion

	public sealed class ComSpec
	{
		public eComBaudRates BaudRate { get; set; }
		public eComDataBits NumberOfDataBits { get; set; }
		public eComParityType ParityType { get; set; }
		public eComStopBits NumberOfStopBits { get; set; }
		public eComProtocolType ProtocolType { get; set; }
		public eComHardwareHandshakeType HardwareHandshake { get; set; }
		public eComSoftwareHandshakeType SoftwareHandshake { get; set; }
		public bool ReportCtsChanges { get; set; }

		/// <summary>
		/// Constructor.
		/// </summary>
		public ComSpec()
		{
			// Very common defaults, also avoids the problem of defaulting to values
			// that are not part of the enumeration.
			BaudRate = eComBaudRates.BaudRate9600;
			NumberOfDataBits = eComDataBits.DataBits8;
			ParityType = eComParityType.None;
			NumberOfStopBits = eComStopBits.StopBits1;
			ProtocolType = eComProtocolType.Rs232;
			HardwareHandshake = eComHardwareHandshakeType.None;
			SoftwareHandshake = eComSoftwareHandshakeType.None;
			ReportCtsChanges = false;
		}

		/// <summary>
		/// Returns a new copy of the com spec.
		/// </summary>
		/// <returns></returns>
		public ComSpec Copy()
		{
			ComSpec comSpec = new ComSpec();
			comSpec.Copy(this);
			return comSpec;
		}

		/// <summary>
		/// Copies the values from the given com spec instance.
		/// </summary>
		/// <param name="comSpec"></param>
		public void Copy(ComSpec comSpec)
		{
			if (comSpec == null)
				throw new ArgumentNullException("comSpec");

			BaudRate = comSpec.BaudRate;
			NumberOfDataBits = comSpec.NumberOfDataBits;
			ParityType = comSpec.ParityType;
			NumberOfStopBits = comSpec.NumberOfStopBits;
			ProtocolType = comSpec.ProtocolType;
			HardwareHandshake = comSpec.HardwareHandshake;
			SoftwareHandshake = comSpec.SoftwareHandshake;
			ReportCtsChanges = comSpec.ReportCtsChanges;
		}
	}
}
