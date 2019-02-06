using System;

namespace ICD.Connect.Protocol.Network.Direct
{
    public interface IReply : IMessage
    {
		/// <summary>
		/// The ID of the initial message that is being replied to.
		/// </summary>
		Guid OriginalMessageId { get; set; }
    }
}
