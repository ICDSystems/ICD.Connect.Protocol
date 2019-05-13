using System;
using ICD.Common.Utils;
using ICD.Connect.Protocol.Ports;
using Newtonsoft.Json;

namespace ICD.Connect.Protocol.Network.Direct
{
	[JsonConverter(typeof(MessageConverter))]
	public sealed class Message
	{
		/// <summary>
		/// The ID of the initial message that is being replied to.
		/// </summary>
		public Guid OriginalMessageId { get; set; }

		public Guid Id { get; set; }
		public HostSessionInfo From { get; set; }
		public HostSessionInfo To { get; set; }
		public Type Type { get; set; }
		public object Data { get; set; }

		public override string ToString()
		{
			ReprBuilder builder = new ReprBuilder(this);
			
			if (OriginalMessageId != default(Guid))
				builder.AppendProperty("OriginalMessageId", OriginalMessageId);

			return builder.AppendProperty("Id", Id)
			              .AppendProperty("From", From)
			              .AppendProperty("To", To)
			              .AppendProperty("Type", Type)
			              .ToString();
		}

		/// <summary>
		/// Creates a new message instance with the given data.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static Message FromData(object data)
		{
			if (data == null)
				throw new ArgumentNullException("data");

			return new Message
			{
				Data = data,
				Type = data.GetType()
			};
		}
	}
}