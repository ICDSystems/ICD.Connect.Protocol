using ICD.Common.Utils.Collections;
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
				writer.WriteStartArray();
				{
					foreach (SigInfo sig in value.GetSigs())
						serializer.Serialize(writer, sig);
				}
				writer.WriteEndArray();
			}
		}

		/// <summary>
		/// Reads the JSON representation of the object.
		/// </summary>
		/// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader"/> to read from.</param>
		/// <param name="existingValue">The existing value of object being read.</param>
		/// <param name="serializer">The calling serializer.</param>
		/// <returns>
		/// The object value.
		/// </returns>
		public override CrosspointData ReadJson(JsonReader reader, CrosspointData existingValue, JsonSerializer serializer)
		{
			CrosspointData.eMessageType type = CrosspointData.eMessageType.Message;
			int equipmentId = 0;
			IcdHashSet<int> controlIds = new IcdHashSet<int>();
			IcdHashSet<string> json = new IcdHashSet<string>();
			SigCache sigs = new SigCache();

			while (reader.Read())
			{
				if (reader.TokenType == JsonToken.Null)
					return null;
				
				if (reader.TokenType == JsonToken.EndObject)
				{
					reader.Read();
					break;
				}

				if (reader.TokenType != JsonToken.PropertyName)
					continue;

				string property = reader.Value as string;

				// Read to the value
				reader.Read();

				switch (property)
				{
					case MESSAGE_TYPE_PROPERTY:
						type = reader.GetValueAsEnum<CrosspointData.eMessageType>();
						break;

					case EQUIPMENT_ID_PROPERTY:
						equipmentId = reader.GetValueAsInt();
						break;

					case CONTROL_IDS_PROPERTY:
						while (reader.Read() && reader.TokenType != JsonToken.EndArray)
							controlIds.Add(reader.GetValueAsInt());
						break;

					case JSON_PROPERTY:
						while (reader.Read() && reader.TokenType != JsonToken.EndArray)
							json.Add(reader.GetValueAsString());
						break;

					case SIGS_PROPERTY:
						while (reader.TokenType != JsonToken.EndArray)
							sigs.Add(reader.ReadAsObject<SigInfo>());
						break;
				}
			}

			CrosspointData output = new CrosspointData
			{
				MessageType = type,
				EquipmentId = equipmentId,
			};

			output.AddControlIds(controlIds);
			output.AddJson(json);
			output.AddSigs(sigs);

			return output;
		}

		#endregion
	}
}
