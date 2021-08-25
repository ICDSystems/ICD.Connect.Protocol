#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using System;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.Network.Direct
{
	public sealed class MessageConverter : AbstractGenericJsonConverter<Message>
	{
		private const string ATTR_ORIGINAL_MESSAGE_ID = "oi";
		private const string ATTR_ID = "i";
		private const string ATTR_FROM = "f";
		private const string ATTR_TO = "t";
		private const string ATTR_TYPE = "ty";
		private const string ATTR_DATA = "d";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, Message value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.OriginalMessageId != default(Guid))
				writer.WriteProperty(ATTR_ORIGINAL_MESSAGE_ID, value.OriginalMessageId);

			if (value.Id != default(Guid))
				writer.WriteProperty(ATTR_ID, value.Id);

			if (value.From != default(HostSessionInfo))
			{
				writer.WritePropertyName(ATTR_FROM);
				serializer.Serialize(writer, value.From);
			}

			if (value.To != default(HostSessionInfo))
			{
				writer.WritePropertyName(ATTR_TO);
				serializer.Serialize(writer, value.To);
			}

			if (value.Type != null)
				writer.WriteProperty(ATTR_TYPE, value.Type);

			if (value.Data != null)
			{
				writer.WritePropertyName(ATTR_DATA);
				serializer.Serialize(writer, value.Data);
			}
		}

		/// <summary>
		/// Override to handle the current property value with the given name.
		/// </summary>
		/// <param name="property"></param>
		/// <param name="reader"></param>
		/// <param name="instance"></param>
		/// <param name="serializer"></param>
		protected override void ReadProperty(string property, JsonReader reader, Message instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_ORIGINAL_MESSAGE_ID:
					instance.OriginalMessageId = reader.GetValueAsGuid();
					break;

				case ATTR_ID:
					instance.Id = reader.GetValueAsGuid();
					break;

				case ATTR_FROM:
					instance.From = serializer.Deserialize<HostSessionInfo>(reader);
					break;

				case ATTR_TO:
					instance.To = serializer.Deserialize<HostSessionInfo>(reader);
					break;

				case ATTR_TYPE:
					instance.Type = reader.GetValueAsType();
					break;

				case ATTR_DATA:
					instance.Data = serializer.Deserialize(reader, instance.Type);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
