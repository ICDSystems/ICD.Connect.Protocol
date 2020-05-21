using System;
using ICD.Common.Utils.Services;
using ICD.Connect.Settings.Cores;

namespace ICD.Connect.Protocol.Network.Direct
{
	public abstract class AbstractMessageHandler : IMessageHandler
	{
		/// <summary>
		/// Raised to send messages back to a connected endpoint.
		/// </summary>
		public event ReplyCallback OnAsyncReply;

		/// <summary>
		/// Gets the message type that this handler is expecting.
		/// </summary>
		public abstract Type MessageType { get; }

		protected ICore Core { get { return ServiceProvider.GetService<ICore>(); } }

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
		/// Handles the message receieved from the remote core.
		/// </summary>
		/// <param name="message"></param>
		/// <returns>Returns an AbstractMessage as a reply, or null for no reply</returns>
		public abstract Message HandleMessage(Message message);

		/// <summary>
		/// Returns a reply to the tagged client.
		/// </summary>
		/// <param name="reply"></param>
		protected void RaiseReply(Message reply)
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
