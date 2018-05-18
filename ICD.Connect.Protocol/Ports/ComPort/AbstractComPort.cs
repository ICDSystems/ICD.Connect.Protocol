using System;
using ICD.Connect.Protocol.Settings;

namespace ICD.Connect.Protocol.Ports.ComPort
{
	public abstract class AbstractComPort<TSettings> : AbstractSerialPort<TSettings>, IComPort
		where TSettings : IComPortSettings, new()
	{
		/// <summary>
		/// Gets the Com Spec configuration properties.
		/// </summary>
		protected abstract IComSpecProperties ComSpecProperties { get; }

		#region Methods

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
		/// Returns the connection state of the port.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsConnectedState()
		{
			return true;
		}

		/// <summary>
		/// Configures the ComPort for communication.
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

		/// <summary>
		/// Applies the given device configuration properties to the port.
		/// </summary>
		/// <param name="properties"></param>
		public void ApplyDeviceConfiguration(IComSpecProperties properties)
		{
			if (properties == null)
				throw new ArgumentNullException("properties");

			// Port supercedes device configuration
			IComSpecProperties config = ComSpecProperties.Superimpose(properties);

			ApplyConfiguration(config);
		}

		/// <summary>
		/// Applies the given configuration properties to the port.
		/// </summary>
		/// <param name="properties"></param>
		public void ApplyConfiguration(IComSpecProperties properties)
		{
			if (properties == null)
				throw new ArgumentNullException("properties");

			SetComPortSpec(properties.ComSpecBaudRate ?? default(eComBaudRates),
			               properties.ComSpecNumberOfDataBits ?? default(eComDataBits),
			               properties.ComSpecParityType ?? default(eComParityType),
			               properties.ComSpecNumberOfStopBits ?? default(eComStopBits),
			               properties.ComSpecProtocolType ?? default(eComProtocolType),
			               properties.ComSpecHardwareHandShake ?? default(eComHardwareHandshakeType),
			               properties.ComSpecSoftwareHandshake ?? default(eComSoftwareHandshakeType),
			               properties.ComSpecReportCtsChanges ?? false);
		}

		#endregion
	}
}
