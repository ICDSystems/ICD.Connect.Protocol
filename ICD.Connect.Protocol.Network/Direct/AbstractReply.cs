using System;
using Newtonsoft.Json;

namespace ICD.Connect.Protocol.Network.Direct
{
	public abstract class AbstractReply : AbstractMessage, IReply
	{
		/// <summary>
		/// The ID of the initial message that is being replied to.
		/// </summary>
		[JsonProperty("oi", DefaultValueHandling = DefaultValueHandling.Ignore)]
		public Guid OriginalMessageId { get; set; }
	}
}
