using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
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

		private readonly SafeCriticalSection m_SendingSection;
		private readonly SafeTimer m_PulseTimer;
		private readonly SafeTimer m_BetweenTimer;
		private readonly Queue<IrPulse> m_Queue;

		private bool m_IsSending;
		private IrPulse m_CurrentCommand;

		#endregion

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		public IrPortPulseComponent([NotNull] Action<string> press, [NotNull] Action release)
		{
			if (press == null)
				throw new ArgumentNullException("press");
			if (release == null)
				throw new ArgumentNullException("release");

			m_PressAction = press;
			m_ReleaseAction = release;

			m_SendingSection = new SafeCriticalSection();
			m_PulseTimer = SafeTimer.Stopped(PulseElapseCallback);
			m_BetweenTimer = SafeTimer.Stopped(BetweenElapsedCallback);
			m_Queue = new Queue<IrPulse>();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			Clear();
			m_PulseTimer.Dispose();
			m_BetweenTimer.Dispose();
		}

		#endregion

		#region Methods

		public void EnqueuePulse(IrPulse pulse)
		{
			m_SendingSection.Enter();

			try
			{
				m_Queue.Enqueue(pulse);

				if (m_IsSending || m_Queue.Count != 1)
					return;
			}
			finally
			{
				m_SendingSection.Leave();
			}

			SendNext();
		}

		/// <summary>
		/// Releases the current command and clears the queued commands.
		/// </summary>
		public void Clear()
		{
			m_ReleaseAction();

			m_SendingSection.Enter();

			try
			{
				m_PulseTimer.Stop();
				m_BetweenTimer.Stop();
				m_Queue.Clear();
			}
			finally
			{
				m_SendingSection.Leave();
			}
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Sends the next pulse in the queue.
		/// </summary>
		private void SendNext()
		{
			IrPulse pulse;


			m_SendingSection.Enter();

			try
			{
				if (m_IsSending || m_Queue.Count == 0)
					return;


				if (!m_Queue.Dequeue(out pulse))
					return;

				m_IsSending = true;
				m_CurrentCommand = pulse;
			}
			finally
			{
				m_SendingSection.Leave();
			}

			// Be sure the timer always runs or component will deadlock
			try
			{
				m_ReleaseAction();
				m_PressAction(pulse.Command);
			}
			finally
			{
				m_PulseTimer.Reset(pulse.PulseTime);
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
				m_BetweenTimer.Reset(m_CurrentCommand.BetweenTime);
			}
		}

		/// <summary>
		/// Called when the between timer elapses
		/// </summary>
		private void BetweenElapsedCallback()
		{
			m_SendingSection.Enter();

			try
			{
				m_IsSending = false;
				m_CurrentCommand = default;

				if (m_Queue.Count == 0)
					return;
			}
			finally
			{
				m_SendingSection.Leave();
			}

			SendNext();
		}

		#endregion
	}
}