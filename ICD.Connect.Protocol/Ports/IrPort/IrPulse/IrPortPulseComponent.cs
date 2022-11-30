using System;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;

namespace ICD.Connect.Protocol.Ports.IrPort.IrPulse
{
	public sealed class IrPortPulseComponent : IDisposable
	{
		#region Members

		[NotNull]
		private readonly Action<string> m_PressAction;

		[NotNull]
		private readonly Action m_ReleaseAction;
		
		private readonly SafeTimer m_PulseTimer;
		private readonly ThreadedWorkerQueue<IrPulse> m_Queue;
		private readonly IcdAutoResetEvent m_ReleaseEvent;

		#endregion
		
		#region Properties

		/// <summary>
		/// Change if the process to run the commands is running or not
		/// Typically drive by the online state of the IR port
		/// </summary>
		public bool RunProcess
		{
			get { return m_Queue.RunProcess; }
			set { m_Queue.SetRunProcess(value); }
		}

		#endregion

		#region Constructor

		/// <summary>
		/// Creates a new IR Pulse component with the given press and release actions.
		/// </summary>
		/// <param name="press">Action to call to press, with name of the IR command</param>
		/// <param name="release">Action to call to release</param>
		/// <param name="runProcess">If True, processes the que initially</param>
		/// <exception cref="ArgumentNullException"></exception>
		public IrPortPulseComponent([NotNull] Action<string> press, [NotNull] Action release, bool runProcess)
		{
			if (press == null)
				throw new ArgumentNullException("press");
			if (release == null)
				throw new ArgumentNullException("release");

			m_PressAction = press;
			m_ReleaseAction = release;
			
			m_PulseTimer = SafeTimer.Stopped(PulseElapseCallback);
			m_Queue = new ThreadedWorkerQueue<IrPulse>(ProcessItem, runProcess);
			m_ReleaseEvent = new IcdAutoResetEvent(false);
		}

		public IrPortPulseComponent([NotNull] Action<string> press, [NotNull] Action release) : this(press, release,
			true)
		{
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			Clear();
			m_PulseTimer.Dispose();
			m_ReleaseEvent.Dispose();
		}

		#endregion

		#region Methods

		public void EnqueuePulse(IrPulse pulse)
		{
			m_Queue.Enqueue(pulse);
		}

		/// <summary>
		/// Releases the current command and clears the queued commands.
		/// </summary>
		public void Clear()
		{
			m_ReleaseAction();

			m_Queue.Clear();
			m_PulseTimer.Stop();
		}

		#endregion

		#region Private Methods
		
		private void ProcessItem(IrPulse pulse)
		{
			// Be sure the timer always runs or component will deadlock
			try
			{
				m_ReleaseAction();
				
				// Set queue between time based on this value
				m_Queue.BetweenTime = pulse.BetweenTime;
				m_PressAction(pulse.Command);
			}
			finally
			{
				m_PulseTimer.Reset(pulse.PulseTime);
				
				// Wait for the pulse release, to make the queue wait for the release before it starts the between time
				// Wait for 5x the pulse time or at least one second, just to be safe
				
				if (!m_ReleaseEvent.WaitOne(Math.Max(1000, pulse.PulseTime * 5)))
					ServiceProvider.TryGetService<ILoggerService>().AddEntry(eSeverity.Error, "Timeout waiting for IR release event to signal");
			}
		}

		/// <summary>
		/// Called when the pulse timer elapses.
		/// </summary>
		private void PulseElapseCallback()
		{
			// Be sure the timer always runs or component will deadlock
			try
			{
				m_ReleaseAction();
			}
			finally
			{
				m_ReleaseEvent.Set();
			}
		}

		#endregion
	}
}