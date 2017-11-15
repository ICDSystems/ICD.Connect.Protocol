using System;
using System.Globalization;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.Ports;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ICD.Connect.Protocol.Network.Broadcast
{
	public class Broadcast : ISerialData
	{
		#region Properties

		/// <summary>
		/// The type of the data being advertised. Used for deserialization.
		/// </summary>
		public string Type { get { return Data.GetType().AssemblyQualifiedName; } }

		/// <summary>
		/// The source of this advertisement.
		/// </summary>
		public HostInfo Source { get; private set; }

		/// <summary>
		/// The data that is being advertised.
		/// </summary>
		public object Data { get; private set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="data"></param>
		public Broadcast(HostInfo source, object data)
		{
			Source = source;
			Data = data;
		}

		#region Methods

		/// <summary>
		/// Deserializes the advertisement from a JSON string.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static Broadcast Deserialize(string str)
		{
			JObject o = JObject.Parse(str);
			Type type = System.Type.GetType(o.SelectToken("Type").ToString());
			HostInfo source = JsonConvert.DeserializeObject<HostInfo>(o.SelectToken("Source").ToString());
			JToken obj = o.SelectToken("Data");
			object data;

			if (type == null)
				type = typeof(string);
			if (!(obj is JValue))
				data = JsonConvert.DeserializeObject(obj.ToString(), type);
			else
				data = Convert.ChangeType(obj.ToString(), type, CultureInfo.InvariantCulture);
			return new Broadcast(source, data);
		}

		/// <summary>
		/// Serializes the advertisement to a JSON string.
		/// </summary>
		/// <returns></returns>
		public string Serialize()
		{
			return JsonConvert.SerializeObject(this, Formatting.None,
			                                   new JsonSerializerSettings
			                                   {
				                                   DefaultValueHandling = DefaultValueHandling.Ignore
			                                   });
		}

		#endregion
	}

	public sealed class Broadcast<T> : Broadcast
	{
		public new T Data { get { return (T)base.Data; } }

		public Broadcast(HostInfo source, T data)
			: base(source, data)
		{
		}

		public Broadcast(Broadcast b)
			: base(b.Source, b.Data)
		{
		}
	}
}
