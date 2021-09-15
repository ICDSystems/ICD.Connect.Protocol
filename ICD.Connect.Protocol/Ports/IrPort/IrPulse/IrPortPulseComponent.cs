using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;

namespace ICD.Connect.Protocol.Ports.IrPort.IrPulse
{
	public sealed class IrPortPulseComponent : IDisposable
	{
		#region Members

		[NotNull]
		private readonly IIrPort m_Port;

		private readonly SafeCriticalSection m_PulseSection;
		private readonly SafeTimer m_PulseTimer;
		private readonly SafeTimer m_BetweenTimer;
		private readonly Queue<IrPulse> m_Queue;

		private bool m_IsSending;
		private ushort m_BetweenTime;

		#endregion

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		public IrPortPulseComponent([NotNull] IIrPort parent)
		{
			if (parent == null)
				throw new ArgumentNullException("parent");

			m_Port = parent;

			m_PulseSection = new SafeCriticalSection();
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
			m_PulseSection.Enter();

			try
			{
				m_Queue.Enqueue(pulse);

				if (m_IsSending || m_Queue.Count != 1)
					return;
			}
			finally
			{
				m_PulseSection.Leave();
			}

			SendNext();
		}

		/// <summary>
		/// Releases the current command and clears the queued commands.
		/// </summary>
		public void Clear()
		{
			m_Port.Release();

			m_PulseSection.Enter();

			try
			{
				m_PulseTimer.Stop();
				m_BetweenTimer.Stop();
				m_Queue.Clear();
			}
			finally
			{
				m_PulseSection.Leave();
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


			m_PulseSection.Enter();

			try
			{
				if (m_IsSending || m_Queue.Count == 0)
					return;


				if (!m_Queue.Dequeue(out pulse))
					return;

				m_IsSending = true;
			}
			finally
			{
				m_PulseSection.Leave();
			}

			m_BetweenTime = pulse.BetweenTime;

			m_Port.Release();
			m_Port.Press(pulse.Command);
			m_PulseTimer.Reset(pulse.PulseTime);


		}

		/// <summary>
		/// Called when the pulse timer elapses.
		/// </summary>
		private void PulseElapseCallback()
		{
			m_Port.Release();

			m_BetweenTimer.Reset(m_BetweenTime);
		}

		/// <summary>
		/// Called when the between timer elapses
		/// </summary>
		private void BetweenElapsedCallback()
		{
			m_PulseSection.Enter();

			try
			{
				m_IsSending = false;

				if (m_Queue.Count == 0)
					return;
			}
			finally
			{
				m_PulseSection.Leave();
			}

			SendNext();
		}

		#endregion
	}
}