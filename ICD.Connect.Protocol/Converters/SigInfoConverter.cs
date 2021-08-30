#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using System;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.Sigs;

namespace ICD.Connect.Protocol.Converters
{
	public sealed class SigInfoConverter : JsonConverter
	{
		// JSON
		private const string TYPE_PROPERTY = "T";
		private const string NUMBER_PROPERTY = "No";
		private const string NAME_PROPERTY = "Na";
		private const string SMARTOBJECT_PROPERTY = "SO";
		private const string VALUE_PROPERTY = "V";

		/// <summary>
		/// Determines whether this instance can convert the specified object type.
		/// </summary>
		/// <param name="objectType">Type of the object.</param>
		/// <returns>
		/// <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
		/// </returns>
		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(SigInfo);
		}

		/// <summary>
		/// Writes the JSON representation of the object.
		/// </summary>
		/// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter"/> to write to.</param><param name="value">The value.</param><param name="serializer">The calling serializer.</param>
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			if (serializer == null)
				throw new ArgumentNullException("serializer");

			SigInfo sigInfo = (SigInfo)value;

			writer.WriteStartObject();
			{
				writer.WritePropertyName(TYPE_PROPERTY);
				writer.WriteValue(sigInfo.Type.ToString());

				if (sigInfo.Number != 0)
				{
					writer.WritePropertyName(NUMBER_PROPERTY);
					writer.WriteValue(sigInfo.Number);
				}

				if (!string.IsNullOrEmpty(sigInfo.Name))
				{
					writer.WritePropertyName(NAME_PROPERTY);
					writer.WriteValue(sigInfo.Name);
				}

				if (sigInfo.SmartObject != 0)
				{
					writer.WritePropertyName(SMARTOBJECT_PROPERTY);
					writer.WriteValue(sigInfo.SmartObject);
				}

				if (sigInfo.HasValue())
				{
					writer.WritePropertyName(VALUE_PROPERTY);
					switch (sigInfo.Type)
					{
						case eSigType.Digital:
							writer.WriteValue(sigInfo.GetBoolValue());
							break;
						case eSigType.Analog:
							writer.WriteValue(sigInfo.GetUShortValue());
							break;
						case eSigType.Serial:
							writer.WriteValue(sigInfo.GetStringValue());
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}
			}
			writer.WriteEndObject();
		}

		/// <summary>
		/// Reads the JSON representation of the object.
		/// </summary>
		/// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader"/> to read from.</param><param name="objectType">Type of the object.</param><param name="existingValue">The existing value of object being read.</param><param name="serializer">The calling serializer.</param>
		/// <returns>
		/// The object value.
		/// </returns>
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");

			if (!CanConvert(objectType))
				throw new ArgumentException("objectType");

			if (serializer == null)
				throw new ArgumentNullException("serializer");

			eSigType type = eSigType.Na;
			uint number = 0;
			string name = null;
			ushort smartObject = 0;

			bool boolValue = false;
			ushort ushortValue = 0;
			string stringValue = null;

			while (reader.Read())
			{
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
					case TYPE_PROPERTY:
						type = reader.GetValueAsEnum<eSigType>();
						break;

					case NUMBER_PROPERTY:
						number = (uint)reader.GetValueAsInt();
						break;

					case NAME_PROPERTY:
						name = reader.GetValueAsString();
						break;

					case SMARTOBJECT_PROPERTY:
						smartObject = (ushort)reader.GetValueAsInt();
						break;

					case VALUE_PROPERTY:
						switch (reader.TokenType)
						{
							case JsonToken.Boolean:
								boolValue = reader.GetValueAsBool();
								break;

							case JsonToken.Integer:
								ushortValue = (ushort)reader.GetValueAsInt();
								break;

							case JsonToken.String:
							case JsonToken.Null:
								stringValue = reader.GetValueAsString();
								break;

							default:
								throw new ArgumentOutOfRangeException();
						}
						break;
				}
			}

			switch (type)
			{
				case eSigType.Digital:
					return new SigInfo(number, name, smartObject, boolValue);
				case eSigType.Analog:
					return new SigInfo(number, name, smartObject, ushortValue);
				case eSigType.Serial:
					return new SigInfo(number, name, smartObject, stringValue);

				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}
