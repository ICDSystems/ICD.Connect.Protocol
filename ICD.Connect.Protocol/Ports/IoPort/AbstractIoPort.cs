using System;
using System.Collections.Generic;
using ICD.Common.Logging.Activities;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Ports.DigitalInput;
using ICD.Connect.Settings;
using ICD.Common.Logging.LoggingContexts;

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
				try
				{
					if (value == m_DigitalIn)
						return;

					m_DigitalIn = value;

					Logger.LogSetTo(eSeverity.Informational, "DigitalIn", m_DigitalIn);

					OnDigitalInChanged.Raise(this, new BoolEventArgs(m_DigitalIn));
				}
				finally
				{
					Activities.LogActivity(m_DigitalIn
						                       ? new Activity(Activity.ePriority.Medium, "Digital In", "Input High",
						                                      eSeverity.Informational)
						                       : new Activity(Activity.ePriority.Low, "Digital In", "Input Low", eSeverity.Informational));
				}
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
				try
				{
					if (value == m_DigitalOut)
						return;

					m_DigitalOut = value;

					Logger.LogSetTo(eSeverity.Informational, "DigitalOut", m_DigitalOut);

					OnDigitalOutChanged.Raise(this, new BoolEventArgs(m_DigitalOut));
				}
				finally
				{
					Activities.LogActivity(m_DigitalOut
						                       ? new Activity(Activity.ePriority.Medium, "Digital Out", "Output High",
						                                      eSeverity.Informational)
						                       : new Activity(Activity.ePriority.Low, "Digital Out", "Output Low", eSeverity.Informational));
				}
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

				Logger.LogSetTo(eSeverity.Informational, "AnalogIn", m_AnalogIn);

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

				Logger.LogSetTo(eSeverity.Informational, "Configuration", m_Configuration);

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
		/// Constructor.
		/// </summary>
		protected AbstractIoPort()
		{
			// Initialize activities
			DigitalIn = false;
			DigitalOut = false;
		}

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

		#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(T settings)
		{
			base.CopySettingsFinal(settings);

			settings.Configuration = Configuration;
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(T settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			if (settings.Configuration != eIoPortConfiguration.None)
				SetConfiguration(settings.Configuration);
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
