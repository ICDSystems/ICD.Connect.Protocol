using System;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
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
		/// Override to handle the current property value with the given name.
		/// </summary>
		/// <param name="property"></param>
		/// <param name="reader"></param>
		/// <param name="instance"></param>
		/// <param name="serializer"></param>
		protected override void ReadProperty(string property, JsonReader reader, SigCache instance, JsonSerializer serializer)
		{
			eSigType sigType = EnumUtils.Parse<eSigType>(property, true);

			reader.ReadObject(serializer, (p, r, s) => ReadType(p, r, instance, s, sigType));
		}

		private void ReadType(string property, JsonReader reader, SigCache instance, JsonSerializer serializer,
		                      eSigType sigType)
		{
			ushort smartObject = ushort.Parse(property);

			reader.ReadObject(serializer, (p, r, s) => ReadSmartObject(p, r, instance, s, sigType, smartObject));
		}

		private void ReadSmartObject(string property, JsonReader reader, SigCache instance, JsonSerializer serializer,
		                             eSigType sigType, ushort smartObject)
		{
			uint number = uint.Parse(property);

			SigInfo info;

			switch (sigType)
			{
				case eSigType.Digital:
					info = new SigInfo(number, smartObject, reader.GetValueAsBool());
					break;

				case eSigType.Analog:
					info = new SigInfo(number, smartObject, (ushort)reader.GetValueAsInt());
					break;

				case eSigType.Serial:
					info = new SigInfo(number, smartObject, reader.GetValueAsString());
					break;

				default:
					throw new ArgumentOutOfRangeException("sigType");
			}

			instance.Add(info);
		}
	}
}
