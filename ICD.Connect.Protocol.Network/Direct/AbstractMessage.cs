using System;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.Ports;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ICD.Connect.Protocol.Network.Direct
{
	[Serializable]
	public abstract class AbstractMessage : IMessage
	{
		public const char DELIMITER = (char)0xff;

		public string Type { get { return GetType().GetNameWithoutAssemblyDetails(); } }

		public Guid MessageId { get; set; }

		public HostSessionInfo MessageFrom { get; set; }

		public HostSessionInfo MessageTo { get; set; }

		public override string ToString()
		{
			return new ReprBuilder(this).AppendProperty("Id", MessageId)
			                            .AppendProperty("From", MessageFrom)
			                            .AppendProperty("To", MessageTo)
			                            .ToString();
		}

		public string Serialize()
		{
			return
				JsonConvert.SerializeObject(this, Formatting.None,
				                            new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.Auto}) + DELIMITER;
		}

		public static AbstractMessage Deserialize(string serial)
		{
			JObject obj = JObject.Parse(serial);

			Type type = System.Type.GetType(obj.SelectToken("Type").ToString());
			if (type == null)
				return null;

			return
				JsonConvert.DeserializeObject(serial, type, new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.Auto})
				as AbstractMessage;
		}
	}
}