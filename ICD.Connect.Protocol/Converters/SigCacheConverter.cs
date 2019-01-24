using System;
using ICD.Common.Utils.Json;
using ICD.Connect.Protocol.Sigs;
using Newtonsoft.Json;

namespace ICD.Connect.Protocol.Converters
{
	public sealed class SigCacheConverter : AbstractGenericJsonConverter<SigCache>
	{
		/// <summary>
		/// Writes the JSON representation of the object.
		/// </summary>
		/// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter"/> to write to.</param>
		/// <param name="value">The value.</param>
		/// <param name="serializer">The calling serializer.</param>
		public override void WriteJson(JsonWriter writer, SigCache value, JsonSerializer serializer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			if (serializer == null)
				throw new ArgumentNullException("serializer");

			if (value == null)
			{
				writer.WriteNull();
				return;
			}

			writer.WriteStartObject();
			{
				foreach (var typeKvp in value.KeyToSig)
				{
					writer.WritePropertyName(typeKvp.Key.ToString());
					writer.WriteStartObject();
					{
						foreach (var soKvp in typeKvp.Value)
						{
							writer.WritePropertyName(soKvp.Key.ToString());
							writer.WriteStartObject();
							{
								foreach (var numKvp in soKvp.Value)
								{
									writer.WritePropertyName(numKvp.Key.ToString());
									writer.WriteValue(numKvp.Value);
								}
							}
							writer.WriteEndObject();
						}
					}
					writer.WriteEndObject();
				}
			}
			writer.WriteEndObject();
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
		public override SigCache ReadJson(JsonReader reader, SigCache existingValue, JsonSerializer serializer)
		{
			return base.ReadJson(reader, existingValue, serializer);
		}
	}
}
