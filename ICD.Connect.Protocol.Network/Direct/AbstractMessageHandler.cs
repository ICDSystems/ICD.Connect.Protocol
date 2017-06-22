using System;

namespace ICD.Connect.Protocol.Network.Direct
{
	public abstract class AbstractMessageHandler<T> : IMessageHandler, IDisposable where T : AbstractMessage
	{
		/// <summary>
		/// Handles the message receieved
		/// </summary>
		/// <param name="message"></param>
		/// <returns>Returns an AbstractMessage as a reply, or null for no reply</returns>
		public abstract AbstractMessage HandleMessage(T message);

		public AbstractMessage HandleMessage(object message)
		{
			return HandleMessage((T)message);
		}

		public void Dispose()
		{
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing)
		{
		}
	}
}
