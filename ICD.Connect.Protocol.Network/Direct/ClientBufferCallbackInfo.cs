using System;
using ICD.Common.Utils.Timers;

namespace ICD.Connect.Protocol.Network.Direct
{
	public sealed class ClientBufferCallbackInfo : IDisposable
	{
		private readonly SafeTimer m_Timer;

		private Action<IReply> m_HandleReply;
		private bool m_Handled;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="handleReply"></param>
		/// <param name="handleTimeout"></param>
		public ClientBufferCallbackInfo(IMessage message, Action<IReply> handleReply, Action<IMessage> handleTimeout)
		{
			m_HandleReply = handleReply;
			m_Timer = SafeTimer.Stopped(() => HandleTimeout(message, handleTimeout));
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
			m_Handled = true;
			m_HandleReply(reply);
		}

		private void HandleTimeout(IMessage message, Action<IMessage> handleTimeout)
		{
			// Don't timeout if the reply has already been handled.
			if (m_Handled)
				return;

			handleTimeout(message);
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