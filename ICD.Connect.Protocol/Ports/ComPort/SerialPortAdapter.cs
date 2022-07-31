#if NETSTANDARD
using System;
using System.IO.Ports;
using ICD.Common.Properties;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Settings;
using ICD.Connect.Protocol.Utils;
using ICD.Connect.Settings;

namespace ICD.Connect.Protocol.Ports.ComPort
{
	public sealed class SerialPortAdapter : AbstractComPort<SerialPortAdapterSettings>
	{
		private const eComBaudRates DEFAULT_BAUD_RATE = eComBaudRates.BaudRate9600;
		private const eComDataBits DEFAULT_DATA_BITS = eComDataBits.DataBits8;
		private const eComParityType DEFAULT_PARITY_TYPE = eComParityType.None;
		private const eComStopBits DEFAULT_STOP_BITS = eComStopBits.StopBits1;
		private const eComProtocolType DEFAULT_PROTOCOL_TYPE = eComProtocolType.Rs232;
		private const eComHardwareHandshakeType DEFAULT_HARDWARE_HANDSHAKE_TYPE = eComHardwareHandshakeType.None;
		private const eComSoftwareHandshakeType DEFAULT_SOFTWARE_HANDSHAKE_TYPE = eComSoftwareHandshakeType.None;

		private static readonly BiDictionary<eComHardwareHandshakeType, Handshake> s_HardwareHandshakeToHandshake =
			new BiDictionary<eComHardwareHandshakeType, Handshake>
			{
				{eComHardwareHandshakeType.None, Handshake.None},
				{eComHardwareHandshakeType.Rts , Handshake.RequestToSend},
				// TODO
				//{eComHardwareHandshakeType.Cts , ???},
				//{eComHardwareHandshakeType.RtsCts , ???}
			};

		private static readonly BiDictionary<eComStopBits, StopBits> s_ComStopBitsToStopBits =
			new BiDictionary<eComStopBits, StopBits>
			{
				{eComStopBits.StopBits1, StopBits.One},
				{eComStopBits.StopBits2, StopBits.Two},
				// TODO
				//{eComStopBits.???, StopBits.OnePointFive}
			};

		private static readonly BiDictionary<eComParityType, Parity> s_ComParityToParity =
			new BiDictionary<eComParityType, Parity>
			{
				{eComParityType.None, Parity.None},
				{eComParityType.Even , Parity.Even},
				{eComParityType.Odd , Parity.Odd},
				// TODO
				//{eComParityType.Mark , Parity.???},
				//{eComParityType.Even??? , Parity.Space}
			};

		private readonly ComSpecProperties m_ComSpecProperties;

		[CanBeNull]
		private SerialPort m_Port;

		#region Properties

		/// <summary>
		/// Gets the Com Spec configuration properties.
		/// </summary>
		public override IComSpecProperties ComSpecProperties { get { return m_ComSpecProperties; } }

		/// <summary>
		/// Gets the baud rate.
		/// </summary>
		public override eComBaudRates BaudRate
		{
			get { return m_Port == null ? DEFAULT_BAUD_RATE : ComSpecUtils.BaudRateFromRate(m_Port.BaudRate); }
		}

		/// <summary>
		/// Gets the number of data bits.
		/// </summary>
		public override eComDataBits NumberOfDataBits
		{
			get { return m_Port == null ? DEFAULT_DATA_BITS : ComSpecUtils.DataBitsFromCount(m_Port.DataBits); }
		}

		/// <summary>
		/// Gets the parity type.
		/// </summary>
		public override eComParityType ParityType { get { return m_Port == null ? DEFAULT_PARITY_TYPE : s_ComParityToParity.GetKey(m_Port.Parity); } }

		/// <summary>
		/// Gets the number of stop bits.
		/// </summary>
		public override eComStopBits NumberOfStopBits
		{
			get { return m_Port == null ? DEFAULT_STOP_BITS : s_ComStopBitsToStopBits.GetKey(m_Port.StopBits); }
		}

		/// <summary>
		/// Gets the protocol type.
		/// </summary>
		public override eComProtocolType ProtocolType { get { return DEFAULT_PROTOCOL_TYPE; } }

		/// <summary>
		/// Gets the hardware handshake mode.
		/// </summary>
		public override eComHardwareHandshakeType HardwareHandshake
		{
			get
			{
				return m_Port == null
					? DEFAULT_HARDWARE_HANDSHAKE_TYPE
					: s_HardwareHandshakeToHandshake.GetKey(m_Port.Handshake);
			}
		}

		/// <summary>
		/// Gets the software handshake mode.
		/// </summary>
		public override eComSoftwareHandshakeType SoftwareHandshake
		{
			get
			{
				// TODO
				return DEFAULT_SOFTWARE_HANDSHAKE_TYPE;
			}
		}

		/// <summary>
		/// Gets the report CTS changes mode.
		/// </summary>
		public override bool ReportCtsChanges { get { return true; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public SerialPortAdapter()
		{
			m_ComSpecProperties = new ComSpecProperties();
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			// Unsubscribe and unregister
			SetSerialPort(null);
		}

		/// <summary>
		/// Configures the ComPort for communication.
		/// </summary>
		/// <param name="comSpec"></param>
		[PublicAPI]
		public override void SetComPortSpec(ComSpec comSpec)
		{
			if (m_Port == null)
			{
				Logger.Log(eSeverity.Error, "Unable to set ComSpec - internal port is null");
				return;
			}

			m_Port.BaudRate = ComSpecUtils.BaudRateToRate(comSpec.BaudRate);
			m_Port.DataBits = ComSpecUtils.DataBitsToCount(comSpec.NumberOfDataBits);
			m_Port.Parity = s_ComParityToParity.GetValue(comSpec.ParityType);
			m_Port.StopBits = s_ComStopBitsToStopBits.GetValue(comSpec.NumberOfStopBits);
			m_Port.Handshake = s_HardwareHandshakeToHandshake.GetValue(comSpec.HardwareHandshake);
		}

		/// <summary>
		/// Sets the serial port.
		/// </summary>
		/// <param name="port"></param>
		[PublicAPI]
		public void SetSerialPort(SerialPort port)
		{
			Unsubscribe(m_Port);
			Unregister(m_Port);

			m_Port = port;

			Register(m_Port);
			Subscribe(m_Port);

			UpdateCachedOnlineStatus();
			UpdateIsConnectedState();
		}

		/// <summary>
		/// Connects to the end point.
		/// </summary>
		public override void Connect()
		{
		}

		/// <summary>
		/// Disconnects from the end point.
		/// </summary>
		public override void Disconnect()
		{
		}

		/// <summary>
		/// Implements the actual sending logic. Wrapped by Send to handle connection status.
		/// </summary>
		protected override bool SendFinal(string data)
		{
			if (m_Port == null)
			{
				Logger.Log(eSeverity.Error, "Unable to send data - internal port is null");
				return false;
			}

			PrintTx(() => data);
			m_Port.Write(data);

			return true;
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Unregisters the given port.
		/// </summary>
		/// <param name="port"></param>
		private void Unregister(SerialPort port)
		{
			try
			{
				if (port != null)
					port.Close();
			}
			catch (Exception e)
			{
				Logger.Log(eSeverity.Error, "Failed to close serial port - {0}", e.Message);
			}
		}

		/// <summary>
		/// Registers the port and then re-registers the parent.
		/// </summary>
		/// <param name="port"></param>
		private void Register(SerialPort port)
		{
			try
			{
				if (port != null)
					port.Open();
			}
			catch (Exception e)
			{
				Logger.Log(eSeverity.Error, "Failed to open serial port - {0}", e.Message);
			}
		}

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return m_Port != null && m_Port.IsOpen;
		}

		/// <summary>
		/// Returns the connection state of the port
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsConnectedState()
		{
			return GetIsOnlineStatus();
		}

		#endregion

		#region Port Callbacks

		/// <summary>
		/// Subscribe to the port events.
		/// </summary>
		private void Subscribe(SerialPort port)
		{
			if (port == null)
				return;

			port.DataReceived += PortOnDataReceived;
			port.ErrorReceived += PortOnErrorReceived;
		}

		/// <summary>
		/// Unsubscribe from the port events.
		/// </summary>
		/// <param name="port"></param>
		private void Unsubscribe(SerialPort port)
		{
			if (port == null)
				return;

			port.DataReceived -= PortOnDataReceived;
			port.ErrorReceived -= PortOnErrorReceived;
		}

		private void PortOnErrorReceived(object sender, SerialErrorReceivedEventArgs args)
		{
			UpdateCachedOnlineStatus();
			UpdateIsConnectedState();
		}

		private void PortOnDataReceived(object sender, SerialDataReceivedEventArgs args)
		{
			string data = m_Port == null ? null : m_Port.ReadExisting();
			if (data == null)
				return;

			PrintRx(() => data);
			Receive(data);
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(SerialPortAdapterSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.SerialLine = m_Port == null ? SerialPortAdapterSettings.DEFAULT_SERIAL_LINE : m_Port.PortName;
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			SetSerialPort(null);
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(SerialPortAdapterSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			SerialPort port = null;

			try
			{
				port = new SerialPort(settings.SerialLine);
			}
			catch (Exception e)
			{
				Logger.Log(eSeverity.Error, "Unable to instantiate SerialPort with name {0} - {1}", settings.SerialLine,
				           e.Message);
			}

			SetSerialPort(port);
			ApplyConfiguration();
		}

		#endregion

		#region Console Commands

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("SerialLine", m_Port == null ? null : m_Port.PortName);
		}

		#endregion
	}
}
#endif
