using System;
using ICD.Common.Properties;

namespace ICD.Connect.Protocol.Network.Direct
{
	public delegate void ReplyCallback(IMessageHandler sender, Message reply);

	public interface IMessageHandler : IDisposable
	{
		/// <summary>
		/// Raised to send messages back to a connected endpoint.
		/// </summary>
		event ReplyCallback OnAsyncReply;

		/// <summary>
		/// Gets the message type that this handler is expecting.
		/// </summary>
		Type MessageType { get; }

		/// <summary>
		/// Interprets the incoming message. Returns a reply or null if there is no reply.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		[CanBeNull]
		Message HandleMessage(Message message);
	}
}
