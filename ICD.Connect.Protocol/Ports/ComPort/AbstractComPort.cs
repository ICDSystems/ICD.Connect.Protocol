﻿using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Settings;
using ICD.Connect.Protocol.Utils;
using ICD.Connect.Settings;

namespace ICD.Connect.Protocol.Ports.ComPort
{
	public abstract class AbstractComPort<TSettings> : AbstractSerialPort<TSettings>, IComPort
		where TSettings : IComPortSettings, new()
	{
		#region Properties

		/// <summary>
		/// Gets the Com Spec configuration properties.
		/// </summary>
		[NotNull]
		public abstract IComSpecProperties ComSpecProperties { get; }

		/// <summary>
		/// Gets the baud rate.
		/// </summary>
		public abstract eComBaudRates BaudRate { get; }

		/// <summary>
		/// Gets the number of data bits.
		/// </summary>
		public abstract eComDataBits NumberOfDataBits { get; }

		/// <summary>
		/// Gets the parity type.
		/// </summary>
		public abstract eComParityType ParityType { get; }

		/// <summary>
		/// Gets the number of stop bits.
		/// </summary>
		public abstract eComStopBits NumberOfStopBits { get; }

		/// <summary>
		/// Gets the protocol type.
		/// </summary>
		public abstract eComProtocolType ProtocolType { get; }

		/// <summary>
		/// Gets the hardware handshake mode.
		/// </summary>
		public abstract eComHardwareHandshakeType HardwareHandshake { get; }

		/// <summary>
		/// Gets the software handshake mode.
		/// </summary>
		public abstract eComSoftwareHandshakeType SoftwareHandshake { get; }

		/// <summary>
		/// Gets the report CTS changes mode.
		/// </summary>
		public abstract bool ReportCtsChanges { get; }

		#endregion

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
		/// <param name="comSpec"></param>
		public abstract void SetComPortSpec(ComSpec comSpec);

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
		/// Applies the configuration properties to the port.
		/// </summary>
		public void ApplyConfiguration()
		{
			ApplyConfiguration(ComSpecProperties);
		}

		/// <summary>
		/// Applies the given configuration properties to the port.
		/// </summary>
		/// <param name="properties"></param>
		public void ApplyConfiguration(IComSpecProperties properties)
		{
			if (properties == null)
				throw new ArgumentNullException("properties");

			ComSpec comSpec = new ComSpec
			{
				BaudRate = properties.ComSpecBaudRate ?? BaudRate,
				NumberOfDataBits = properties.ComSpecNumberOfDataBits ?? NumberOfDataBits,
				ParityType = properties.ComSpecParityType ?? ParityType,
				NumberOfStopBits = properties.ComSpecNumberOfStopBits ?? NumberOfStopBits,
				ProtocolType = properties.ComSpecProtocolType ?? ProtocolType,
				HardwareHandshake = properties.ComSpecHardwareHandshake ?? HardwareHandshake,
				SoftwareHandshake = properties.ComSpecSoftwareHandshake ?? SoftwareHandshake,
				ReportCtsChanges = properties.ComSpecReportCtsChanges ?? ReportCtsChanges
			};

			SetComPortSpec(comSpec);
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			ComSpecProperties.ClearComSpecProperties();
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(TSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Copy(ComSpecProperties);
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(TSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			ComSpecProperties.Copy(settings);
		}

		#endregion

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Baud Rate", BaudRate);
			addRow("Number of Data Bits", NumberOfDataBits);
			addRow("Parity Type", ParityType);
			addRow("Number of Stop Bits", NumberOfStopBits);
			addRow("Protocol Type", ProtocolType);
			addRow("Hardware Handshake", HardwareHandshake);
			addRow("Software Handshake", SoftwareHandshake);
			addRow("Report CTS Changes", ReportCtsChanges);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<int>("SetBaudRate", "Sets the baud rate", rate => SetBaudRate(rate));
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		private void SetBaudRate(int baudRate)
		{
			eComBaudRates comBaudRate = ComSpecUtils.BaudRateFromRate(baudRate);

			ComSpec comSpec = new ComSpec
			{
				BaudRate = comBaudRate,
				NumberOfDataBits = NumberOfDataBits,
				ParityType = ParityType,
				NumberOfStopBits = NumberOfStopBits,
				ProtocolType = ProtocolType,
				HardwareHandshake = HardwareHandshake,
				SoftwareHandshake = SoftwareHandshake,
				ReportCtsChanges = ReportCtsChanges
			};

			SetComPortSpec(comSpec);
		}

		#endregion
	}
}
