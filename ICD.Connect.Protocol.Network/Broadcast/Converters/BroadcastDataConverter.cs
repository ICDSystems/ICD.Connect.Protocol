#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.Network.Broadcast.Converters
{
	public sealed class BroadcastDataConverter : AbstractGenericJsonConverter<BroadcastData>
	{
		private const string PROPERTY_SESSION = "session";
		private const string PROPERTY_TYPE = "type";
		private const string PROPERTY_DATA = "data";

		/// <summary>
		/// Creates a new instance of T.
		/// </summary>
		/// <returns></returns>
		protected override BroadcastData Instantiate()
		{
			return new BroadcastData();
		}

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, BroadcastData value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			// Session
			writer.WritePropertyName(PROPERTY_SESSION);
			serializer.Serialize(writer, value.HostSession);

			// Type
			if (value.Type != null)
			{
				writer.WritePropertyName(PROPERTY_TYPE);
				writer.WriteType(value.Type);
			}

			// Value
			if (value.Data != null)
			{
				writer.WritePropertyName(PROPERTY_DATA);
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
		protected override void ReadProperty(string property, JsonReader reader, BroadcastData instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case PROPERTY_SESSION:
					instance.HostSession = serializer.Deserialize<HostSessionInfo>(reader);
					break;

				case PROPERTY_TYPE:
					instance.Type = reader.GetValueAsType();
					break;

				case PROPERTY_DATA:
					instance.Data = serializer.Deserialize(reader, instance.Type);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
