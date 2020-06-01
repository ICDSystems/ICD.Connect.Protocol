using System;
using ICD.Common.Logging.LoggingContexts;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Protocol.Ports.DigitalInput
{
	public abstract class AbstractDigitalInputPort<T> : AbstractPort<T>, IDigitalInputPort
		where T : IDigitalInputPortSettings, new()
	{
		public event EventHandler<BoolEventArgs> OnStateChanged;

		private bool m_State;

		#region Properties

		/// <summary>
		/// Gets the current digital input state.
		/// </summary>
		public bool State
		{
			get { return m_State; }
			protected set
			{
				if (value == m_State)
					return;

				m_State = value;

				Logger.LogSetTo(eSeverity.Informational, "State", m_State);
				Activities.LogActivity(m_State
					                   ? new Activity(Activity.ePriority.Medium, "State", "Input High", eSeverity.Informational)
					                   : new Activity(Activity.ePriority.Low, "State", "Input Low", eSeverity.Informational));

				OnStateChanged.Raise(this, new BoolEventArgs(m_State));
			}
		}

		#endregion

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnStateChanged = null;

			base.DisposeFinal(disposing);
		}

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("State", State);
		}

		#endregion
	}
}
