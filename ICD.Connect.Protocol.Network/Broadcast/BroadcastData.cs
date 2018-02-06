using System;
using System.Globalization;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.Ports;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ICD.Connect.Protocol.Network.Broadcast
{
	public class BroadcastData : ISerialData
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
		public BroadcastData(HostInfo source, object data)
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
		public static BroadcastData Deserialize(string str)
		{
			JObject root = JObject.Parse(str);
			Type type = System.Type.GetType(root.SelectToken("Type").ToString()) ?? typeof(string);
			HostInfo source = JsonConvert.DeserializeObject<HostInfo>(root.SelectToken("Source").ToString());
			JToken obj = root.SelectToken("Data");

			object data;

			if (obj is JValue)
				data = Convert.ChangeType(obj.ToString(), type, CultureInfo.InvariantCulture);
			else
				data = JsonConvert.DeserializeObject(obj.ToString(), type);
			
			return new BroadcastData(source, data);
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

	public sealed class BroadcastData<T> : BroadcastData
	{
		public new T Data { get { return (T)base.Data; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="data"></param>
		public BroadcastData(HostInfo source, T data)
			: base(source, data)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="b"></param>
		public BroadcastData(BroadcastData b)
			: base(b.Source, b.Data)
		{
		}
	}
}
