using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.Settings;
using ICD.Connect.Settings;

namespace ICD.Connect.Protocol.Mock.Ports.ComPort
{
	public sealed class MockComPort : AbstractComPort<MockComPortSettings>
	{
		[PublicAPI]
		public event EventHandler<StringEventArgs> OnSend;

		private readonly ComSpecProperties m_ComSpecProperties;

		private eComBaudRates m_BaudRate;
		private eComDataBits m_NumberOfDataBits;
		private eComParityType m_ParityType;
		private eComStopBits m_NumberOfStopBits;
		private eComProtocolType m_ProtocolType;
		private eComHardwareHandshakeType m_HardwareHandshake;
		private eComSoftwareHandshakeType m_SoftwareHandshake;
		private bool m_ReportCtsChanges;

		#region Properties

		/// <summary>
		/// Gets the Com Spec configuration properties.
		/// </summary>
		public override IComSpecProperties ComSpecProperties { get { return m_ComSpecProperties; } }

		/// <summary>
		/// Gets the baud rate.
		/// </summary>
		public override eComBaudRates BaudRate { get { return m_BaudRate; } }

		/// <summary>
		/// Gets the number of data bits.
		/// </summary>
		public override eComDataBits NumberOfDataBits { get { return m_NumberOfDataBits; } }

		/// <summary>
		/// Gets the parity type.
		/// </summary>
		public override eComParityType ParityType { get { return m_ParityType; } }

		/// <summary>
		/// Gets the number of stop bits.
		/// </summary>
		public override eComStopBits NumberOfStopBits { get { return m_NumberOfStopBits; } }

		/// <summary>
		/// Gets the protocol type.
		/// </summary>
		public override eComProtocolType ProtocolType { get { return m_ProtocolType; } }

		/// <summary>
		/// Gets the hardware handshake mode.
		/// </summary>
		public override eComHardwareHandshakeType HardwareHandshake { get { return m_HardwareHandshake; } }

		/// <summary>
		/// Gets the software handshake mode.
		/// </summary>
		public override eComSoftwareHandshakeType SoftwareHandshake { get { return m_SoftwareHandshake; } }

		/// <summary>
		/// Gets the report CTS changes mode.
		/// </summary>
		public override bool ReportCtsChanges { get { return m_ReportCtsChanges; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public MockComPort()
		{
			m_ComSpecProperties = new ComSpecProperties();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnSend = null;

			base.DisposeFinal(disposing);
		}

		#region Methods

		/// <summary>
		/// Configures the ComPort for communication.
		/// </summary>
		/// <param name="comSpec"></param>
		public override void SetComPortSpec(ComSpec comSpec)
		{
			m_BaudRate = comSpec.BaudRate;
			m_NumberOfDataBits = comSpec.NumberOfDataBits;
			m_ParityType = comSpec.ParityType;
			m_NumberOfStopBits = comSpec.NumberOfStopBits;
			m_ProtocolType = comSpec.ProtocolType;
			m_HardwareHandshake = comSpec.HardwareHandshake;
			m_SoftwareHandshake = comSpec.SoftwareHandshake;
			m_ReportCtsChanges = comSpec.ReportCtsChanges;
		}

		#endregion

		/// <summary>
		/// Implements the actual sending logic. Wrapped by Send to handle connection status.
		/// </summary>
		protected override bool SendFinal(string data)
		{
			EventHandler<StringEventArgs> handler = OnSend;
			if (handler == null)
				return false;

			handler(this, new StringEventArgs(data));

			return true;
		}

		#region Settings

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			ApplyConfiguration();
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(MockComPortSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			ApplyConfiguration();
		}

		#endregion
	}
}
