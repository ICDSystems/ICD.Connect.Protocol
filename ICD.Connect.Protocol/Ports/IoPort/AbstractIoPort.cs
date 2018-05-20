﻿using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Ports.DigitalInput;

namespace ICD.Connect.Protocol.Ports.IoPort
{
	public abstract class AbstractIoPort<T> : AbstractPort<T>, IIoPort
		where T : IIoPortSettings, new()
	{
		public event EventHandler<BoolEventArgs> OnDigitalInChanged;
		public event EventHandler<BoolEventArgs> OnDigitalOutChanged;
		public event EventHandler<UShortEventArgs> OnAnalogInChanged;
		public event IoPortConfigurationCallback OnConfigurationChanged;

		event EventHandler<BoolEventArgs> IDigitalInputPort.OnStateChanged
		{
			add { OnDigitalInChanged += value; }
			// ReSharper disable once DelegateSubtraction
			remove { OnDigitalInChanged -= value; }
		}

		private bool m_DigitalIn;
		private bool m_DigitalOut;
		private ushort m_AnalogIn;
		private eIoPortConfiguration m_Configuration;

		#region Properties

		/// <summary>
		/// Gets the current digital input state.
		/// </summary>
		public bool DigitalIn
		{
			get { return m_DigitalIn; }
			protected set
			{
				if (value == m_DigitalIn)
					return;

				m_DigitalIn = value;

				Log(eSeverity.Informational, "Digital-in changed to {0}", m_DigitalIn);

				OnDigitalInChanged.Raise(this, new BoolEventArgs(m_DigitalIn));
			}
		}

		/// <summary>
		/// Gets the current digital output state.
		/// </summary>
		public bool DigitalOut
		{
			get { return m_DigitalOut; }
			protected set
			{
				if (value == m_DigitalOut)
					return;

				m_DigitalOut = value;

				Log(eSeverity.Informational, "Digital-out changed to {0}", m_DigitalOut);

				OnDigitalOutChanged.Raise(this, new BoolEventArgs(m_DigitalOut));
			}
		}

		/// <summary>
		/// Gets the current analog input state.
		/// </summary>
		public ushort AnalogIn
		{
			get { return m_AnalogIn; }
			protected set
			{
				if (value == m_AnalogIn)
					return;

				m_AnalogIn = value;

				Log(eSeverity.Informational, "Analog-in changed to {0}", m_AnalogIn);

				OnAnalogInChanged.Raise(this, new UShortEventArgs(m_AnalogIn));
			}
		}

		/// <summary>
		/// Gets the current configuration mode.
		/// </summary>
		public eIoPortConfiguration Configuration
		{
			get { return m_Configuration; }
			protected set
			{
				if (value == m_Configuration)
					return;

				m_Configuration = value;

				Log(eSeverity.Informational, "Configuration changed to {0}", m_Configuration);

				IoPortConfigurationCallback handler = OnConfigurationChanged;
				if (handler != null)
					handler(this, m_Configuration);
			}
		}

		/// <summary>
		/// Gets the current digital input state.
		/// </summary>
		bool IDigitalInputPort.State { get { return DigitalIn; } }

		#endregion

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnDigitalInChanged = null;
			OnDigitalOutChanged = null;
			OnAnalogInChanged = null;
			OnConfigurationChanged = null;

			base.DisposeFinal(disposing);
		}

		#region Methods

		/// <summary>
		/// Sets the configuration mode.
		/// </summary>
		public abstract void SetConfiguration(eIoPortConfiguration configuration);

		/// <summary>
		/// Sets the digital output state.
		/// </summary>
		/// <param name="digitalOut"></param>
		public abstract void SetDigitalOut(bool digitalOut);

		#endregion

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Digital In", DigitalIn);
			addRow("Digital Out", DigitalOut);
			addRow("Analog In", AnalogIn);
			addRow("Configuration", Configuration);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			string configurationHelp = string.Format("SetConfiguration <{0}>",
			                                         StringUtils.ArrayFormat(EnumUtils.GetValues<eIoPortConfiguration>()));
			yield return
				new GenericConsoleCommand<eIoPortConfiguration>("SetConfiguration", configurationHelp, a => SetConfiguration(a));
			yield return new GenericConsoleCommand<bool>("SetDigitalOut", "SetDigitalOut <true/false>", a => SetDigitalOut(a));
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
