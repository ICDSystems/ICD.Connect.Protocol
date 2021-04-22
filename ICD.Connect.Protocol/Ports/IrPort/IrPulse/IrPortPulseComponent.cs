using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;

namespace ICD.Connect.Protocol.Ports.IrPort.IrPulse
{
	public sealed class IrPortPulseComponent
	{
		#region Events

		public event EventHandler OnPulseQueueCleared;

		#endregion

		#region Members

		private readonly IIrPort m_Port;

		private readonly SafeCriticalSection m_PulseSection;
		private readonly SafeTimer m_PulseTimer;
		private readonly Queue<IrPulse> m_Queue;

		#endregion

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		public IrPortPulseComponent(IIrPort parent)
		{
			m_Port = parent;

			m_PulseSection = new SafeCriticalSection();
			m_PulseTimer = SafeTimer.Stopped(() => PulseElapseCallback());
			m_Queue = new Queue<IrPulse>();
		}

		#endregion

		#region Methods

		public void EnqueuePulse(IrPulse pulse)
		{
			m_PulseSection.Enter();

			try
			{
				m_Queue.Enqueue(pulse);

				if (m_Queue.Count == 1)
					SendNext();
			}
			finally
			{
				m_PulseSection.Leave();
			}
		}

		/// <summary>
		/// Releases the current command and clears the queued commands.
		/// </summary>
		public void Clear()
		{
			m_PulseSection.Enter();

			try
			{
				m_PulseTimer.Stop();
				m_Queue.Clear();

				OnPulseQueueCleared.Raise(this);
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
			m_PulseSection.Enter();

			try
			{
				IrPulse pulse;
				if (!m_Queue.Peek(out pulse))
					return;

				if (m_Port == null)
				{
					IcdErrorLog.Error("Unable to send command - internal port is null");
					Clear();
					return;
				}

				if (!m_Port.GetCommands().Contains(pulse.Command))
				{
					m_Queue.Dequeue();
					m_Port.Logger.Log(eSeverity.Error, "Unable to send command - No command {0}", StringUtils.ToRepresentation(pulse.Command));
					SendNext();
					return;
				}

				m_Port.Release();
				m_Port.Press(pulse.Command);
				m_PulseTimer.Reset(pulse.Duration);
			}
			finally
			{
				m_PulseSection.Leave();
			}
		}

		/// <summary>
		/// Called when the pulse timer elapses.
		/// </summary>
		private void PulseElapseCallback()
		{
			m_PulseSection.Enter();

			try
			{
				if (m_Queue.Count == 0)
					return;

				m_Queue.Dequeue();
				SendNext();
			}
			finally
			{
				m_PulseSection.Leave();
			}
		}

		#endregion
	}
}