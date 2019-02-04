using System;
using ICD.Common.Utils.Timers;

namespace ICD.Connect.Protocol.Network.Direct
{
	public sealed class ClientBufferCallbackInfo : IDisposable
	{
		private readonly SafeTimer m_Timer;

		private Action<IReply> m_HandleReply;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="handleReply"></param>
		/// <param name="handleTimeout"></param>
		public ClientBufferCallbackInfo(IMessage message, Action<IReply> handleReply, Action<IMessage> handleTimeout)
		{
			m_HandleReply = handleReply;
			m_Timer = SafeTimer.Stopped(() => handleTimeout(message));
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			m_Timer.Dispose();
			m_HandleReply = null;
		}

		/// <summary>
		/// Executes the callback for the given reply.
		/// </summary>
		/// <param name="reply"></param>
		public void HandleReply(IReply reply)
		{
			m_HandleReply(reply);
		}

		/// <summary>
		/// Resets the timeout timer.
		/// </summary>
		/// <param name="timeout"></param>
		public void ResetTimer(long timeout)
		{
			m_Timer.Reset(timeout);
		}
	}
}