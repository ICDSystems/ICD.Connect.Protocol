using System;
using System.Collections.Generic;
using ICD.Common.Logging.Activities;
using ICD.Common.Logging.LoggingContexts;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Protocol.Ports.RelayPort
{
	public abstract class AbstractRelayPort<T> : AbstractPort<T>, IRelayPort
		where T : IRelayPortSettings, new()
	{
		public event EventHandler<BoolEventArgs> OnClosedStateChanged;

		private readonly SafeTimer m_PulseTimer;

		private bool m_Closed;
		private bool m_PulseResult;

		#region Properties

		/// <summary>
		/// Get the state of the relay.
		/// </summary>
		public bool Closed
		{
			get { return m_Closed; }
			protected set
			{
				if (value == m_Closed)
					return;

				m_Closed = value;

				Logger.LogSetTo(eSeverity.Informational, "Closed", m_Closed);
				Activities.LogActivity(m_Closed
					                   ? new Activity(Activity.ePriority.Medium, "Closed", "Closed", eSeverity.Informational)
					                   : new Activity(Activity.ePriority.Medium, "Closed", "Open", eSeverity.Informational));

				OnClosedStateChanged.Raise(this, new BoolEventArgs(m_Closed));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractRelayPort()
		{
			m_PulseTimer = SafeTimer.Stopped(PulseCallback);
		}

		#region Methods

		/// <summary>
		/// Open the relay
		/// </summary>
		public abstract void Open();

		/// <summary>
		/// Close the relay
		/// </summary>
		public abstract void Close();

		/// <summary>
		/// Closes the relay, waits the given duration, then opens the relay.
		/// </summary>
		/// <param name="duration"></param>
		public void PulseOpen(long duration)
		{
			m_PulseResult = true;

			Close();
			m_PulseTimer.Reset(duration);
		}

		/// <summary>
		/// Opens the relay, waits the given duration, then opens the relay.
		/// </summary>
		/// <param name="duration"></param>
		public void PulseClose(long duration)
		{
			m_PulseResult = false;

			Open();
			m_PulseTimer.Reset(duration);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Called at the end of a pulse.
		/// </summary>
		private void PulseCallback()
		{
			if (m_PulseResult)
				Open();
			else
				Close();
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

			addRow("Closed", Closed);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new ConsoleCommand("Open", "Opens the relay", () => Open());
			yield return new ConsoleCommand("Close", "Closes the relay", () => Close());
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
