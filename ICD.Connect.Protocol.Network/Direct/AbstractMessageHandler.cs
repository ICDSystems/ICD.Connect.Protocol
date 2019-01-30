using System;

namespace ICD.Connect.Protocol.Network.Direct
{
	public abstract class AbstractMessageHandler<TMessage, TReply> : IMessageHandler<TMessage, TReply>
		where TMessage : IMessage
		where TReply : IReply
	{
		public event ReplyCallback OnAsyncReply;

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose(bool disposing)
		{
			OnAsyncReply = null;
		}

		/// <summary>
		/// Gets the message type for this handler;
		/// </summary>
		public Type MessageType { get { return typeof(TMessage); } }

		/// <summary>
		/// Interprets the incoming message. Returns a reply or null if there is no reply.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		IReply IMessageHandler.HandleMessage(IMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			return HandleMessage((TMessage)message);
		}

		/// <summary>
		/// Handles the message receieved
		/// </summary>
		/// <param name="message"></param>
		/// <returns>Returns an AbstractMessage as a reply, or null for no reply</returns>
		public abstract TReply HandleMessage(TMessage message);

		/// <summary>
		/// Returns a reply to the tagged client.
		/// </summary>
		/// <param name="reply"></param>
		protected void RaiseReply(TReply reply)
		{
// ReSharper disable once CompareNonConstrainedGenericWithNull
			if (reply == null)
				throw new ArgumentNullException("reply");

			ReplyCallback handler = OnAsyncReply;
			if (handler != null)
				handler(this, reply);
		}
	}
}
