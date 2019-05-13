using System.Linq;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.Ports;
using Newtonsoft.Json;

namespace ICD.Connect.Protocol.Crosspoints.Advertisements
{
	/// <summary>
	/// This is what type of advertisement is being sent.  Mesh and Broadcast are not used yet.
	/// Integer values added to make sure future updates don't completely break compatability between versions with JSON serialization.
	/// </summary>
	public enum eAdvertisementType
	{
		Localhost = 0,
		Multicast = 1,
		Broadcast = 2,
		Directed = 3,
		DirectedRemove = 4,
		Mesh = 5,
		MeshRemove = 6,
		CrosspointRemove = 7
	}

	[JsonConverter(typeof(AdvertisementConverter))]
	public sealed class Advertisement : ISerialData
	{
		#region Properties

		/// <summary>
		/// The source of this advertisement.
		/// </summary>
		public HostInfo Source { get; set; }

		/// <summary>
		/// The control crosspoints that are available.
		/// </summary>
		public CrosspointInfo[] Controls { get; set; }

		/// <summary>
		/// The equipment crosspoints that are available.
		/// </summary>
		public CrosspointInfo[] Equipment { get; set; }

		/// <summary>
		/// The type of advertisement this is
		/// </summary>
		public eAdvertisementType AdvertisementType { get; set; }

		#endregion

		#region Methods

		/// <summary>
		/// Deserializes the advertisement from a JSON string.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static Advertisement Deserialize(string data)
		{
			return JsonConvert.DeserializeObject<Advertisement>(data);
		}

		/// <summary>
		/// Serializes the advertisement to a JSON string.
		/// </summary>
		/// <returns></returns>
		public string Serialize()
		{
			return JsonConvert.SerializeObject(this);
		}

		#endregion
	}

	public sealed class AdvertisementConverter : AbstractGenericJsonConverter<Advertisement>
	{
		private const string ATTR_SOURCE = "s";
		private const string ATTR_CONTROLS = "c";
		private const string ATTR_EQUIPMENT = "e";
		private const string ATTR_ADVERTISEMENT_TYPE = "a";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, Advertisement value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.Source != default(HostInfo))
			{
				writer.WritePropertyName(ATTR_SOURCE);
				serializer.Serialize(writer, value.Source);
			}

			if (value.Controls.Length > 0)
			{
				writer.WritePropertyName(ATTR_CONTROLS);
				serializer.SerializeArray(writer, value.Controls);
			}

			if (value.Equipment.Length > 0)
			{
				writer.WritePropertyName(ATTR_EQUIPMENT);
				serializer.SerializeArray(writer, value.Equipment);
			}

			if (value.AdvertisementType != default(eAdvertisementType))
				writer.WriteProperty(ATTR_ADVERTISEMENT_TYPE, value.AdvertisementType);
		}

		/// <summary>
		/// Override to handle the current property value with the given name.
		/// </summary>
		/// <param name="property"></param>
		/// <param name="reader"></param>
		/// <param name="instance"></param>
		/// <param name="serializer"></param>
		protected override void ReadProperty(string property, JsonReader reader, Advertisement instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_SOURCE:
					instance.Source = serializer.Deserialize<HostInfo>(reader);
					break;

				case ATTR_CONTROLS:
					instance.Controls = serializer.DeserializeArray<CrosspointInfo>(reader).ToArray();
					break;

				case ATTR_EQUIPMENT:
					instance.Equipment = serializer.DeserializeArray<CrosspointInfo>(reader).ToArray();
					break;

				case ATTR_ADVERTISEMENT_TYPE:
					instance.AdvertisementType = reader.GetValueAsEnum<eAdvertisementType>();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
