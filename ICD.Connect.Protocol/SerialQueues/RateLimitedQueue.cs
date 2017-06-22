using System;
using ICD.Common.Utils.Timers;

namespace ICD.Connect.Protocol.SerialQueues
{
	public sealed class RateLimitedQueue : AbstractSerialQueue
	{
		/// <summary>
		/// Wait time between sending commands
		/// </summary>
		public long CommandDelayTime { get; private set; }

		private readonly IcdTimer m_DelayTimer;

		/// <summary>
		/// Creates a queue with 
		/// </summary>
		/// <param name="waitTime"></param>
		public RateLimitedQueue(long waitTime)
		{
			m_DelayTimer = new IcdTimer(waitTime + 1);
			m_DelayTimer.OnElapsed += CommandDelayCallback;

			CommandDelayTime = waitTime;
		}

		public override void Dispose()
		{
			m_DelayTimer.Dispose();

			base.Dispose();
		}

		#region Private Methods

		private void CommandDelayCallback(object sender, EventArgs args)
		{
			if (!IsCommandInProgress && CommandCount > 0)
				SendImmediate();

			if (CommandCount == 0)
				m_DelayTimer.Stop();
			else
				m_DelayTimer.Restart(CommandDelayTime);
		}

		protected override void CommandAdded()
		{
			if (m_DelayTimer.IsRunning)
				return;

			SendImmediate();

			m_DelayTimer.Restart(CommandDelayTime);
		}

		protected override void CommandFinished()
		{
			m_DelayTimer.Restart(CommandDelayTime);
		}

		#endregion
	}
}
