namespace ICD.Connect.Protocol.Ports.ComPort
{
	public abstract class AbstractComPort<TSettings> : AbstractSerialPort<TSettings>, IComPort
		where TSettings : IComPortSettings, new()
	{
		/// <summary>
		/// Sets IsConnected to true.
		/// </summary>
		public override void Connect()
		{
			IsConnected = true;
		}

		/// <summary>
		/// Sets IsConnected to false.
		/// </summary>
		public override void Disconnect()
		{
			IsConnected = false;
		}

		/// <summary>
		/// Returns the connection state of the port
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsConnectedState()
		{
			return true;
		}

		/// <summary>
		/// Configures the com port with the given attributes.
		/// </summary>
		/// <param name="baudRate"></param>
		/// <param name="numberOfDataBits"></param>
		/// <param name="parityType"></param>
		/// <param name="numberOfStopBits"></param>
		/// <param name="protocolType"></param>
		/// <param name="hardwareHandShake"></param>
		/// <param name="softwareHandshake"></param>
		/// <param name="reportCtsChanges"></param>
		public abstract void SetComPortSpec(eComBaudRates baudRate, eComDataBits numberOfDataBits, eComParityType parityType,
		                                    eComStopBits numberOfStopBits, eComProtocolType protocolType,
		                                    eComHardwareHandshakeType hardwareHandShake,
		                                    eComSoftwareHandshakeType softwareHandshake, bool reportCtsChanges);
	}
}
