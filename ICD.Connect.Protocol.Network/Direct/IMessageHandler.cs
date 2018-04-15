using System;
using ICD.Common.Properties;

namespace ICD.Connect.Protocol.Network.Direct
{
	public delegate void ReplyCallback(IMessageHandler sender, IReply reply);

	public interface IMessageHandler : IDisposable
	{
		/// <summary>
		/// Raised to send messages back to a connected endpoint.
		/// </summary>
		event ReplyCallback OnAsyncReply;

		/// <summary>
		/// Interprets the incoming message. Returns a reply or null if there is no reply.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		[CanBeNull]
		IReply HandleMessage(IMessage message);

		/// <summary>
		/// Called to inform the message handler of a client disconnect.
		/// </summary>
		/// <param name="clientId"></param>
		void HandleClientDisconnect(uint clientId);
	}

	public interface IMessageHandler<TMessage, TReply> : IMessageHandler
	{
	}
}
