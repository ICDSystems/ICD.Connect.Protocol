using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using ICD.Connect.Protocol.Sigs;
using Newtonsoft.Json;

namespace ICD.Connect.Protocol.Crosspoints.Converters
{
	public sealed class CrosspointDataConverter : AbstractGenericJsonConverter<CrosspointData>
	{
		// Json
		private const string MESSAGE_TYPE_PROPERTY = "T";
		private const string EQUIPMENT_ID_PROPERTY = "EId";
		private const string CONTROL_IDS_PROPERTY = "CIds";
		private const string JSON_PROPERTY = "J";
		private const string SIGS_PROPERTY = "S";

		#region Serialization

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, CrosspointData value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			writer.WritePropertyName(MESSAGE_TYPE_PROPERTY);
			writer.WriteValue(value.MessageType.ToString());

			if (value.EquipmentId != 0)
			{
				writer.WritePropertyName(EQUIPMENT_ID_PROPERTY);
				writer.WriteValue(value.EquipmentId);
			}

			if (value.ControlIdsCount > 0)
			{
				writer.WritePropertyName(CONTROL_IDS_PROPERTY);
				writer.WriteStartArray();
				{
					foreach (int id in value.GetControlIds())
						writer.WriteValue(id);
				}
				writer.WriteEndArray();
			}

			if (value.JsonCount > 0)
			{
				writer.WritePropertyName(JSON_PROPERTY);
				writer.WriteStartArray();
				{
					foreach (string item in value.GetJson())
						writer.WriteValue(item);
				}
				writer.WriteEndArray();
			}

			if (value.SigsCount > 0)
			{
				writer.WritePropertyName(SIGS_PROPERTY);
				serializer.Serialize(writer, value.SigCache);
			}
		}

		/// <summary>
		/// Override to handle the current property value with the given name.
		/// </summary>
		/// <param name="property"></param>
		/// <param name="reader"></param>
		/// <param name="instance"></param>
		/// <param name="serializer"></param>
		protected override void ReadProperty(string property, JsonReader reader, CrosspointData instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case MESSAGE_TYPE_PROPERTY:
					instance.MessageType = reader.GetValueAsEnum<CrosspointData.eMessageType>();
					break;

				case EQUIPMENT_ID_PROPERTY:
					instance.EquipmentId = reader.GetValueAsInt();
					break;

				case CONTROL_IDS_PROPERTY:
					IEnumerable<int> controlIds = serializer.DeserializeArray<int>(reader);
					instance.AddControlIds(controlIds);
					break;

				case JSON_PROPERTY:
					IEnumerable<string> json = serializer.DeserializeArray<string>(reader);
					instance.AddJson(json);
					break;

				case SIGS_PROPERTY:
					SigCache sigs = serializer.Deserialize<SigCache>(reader);
					instance.AddSigs(sigs);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}

		#endregion
	}
}
