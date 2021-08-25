#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using System;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.Network.Broadcast.Converters;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.Network.Broadcast
{
	[JsonConverter(typeof(BroadcastDataConverter))]
	public sealed class BroadcastData : ISerialData
	{
		#region Properties

		/// <summary>
		/// The source of this advertisement.
		/// </summary>
		public HostSessionInfo HostSession { get; set; }

		/// <summary>
		/// The type of the data being advertised. Used for deserialization.
		/// </summary>
		public Type Type { get; set; }

		/// <summary>
		/// The data that is being advertised.
		/// </summary>
		public object Data { get; set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public BroadcastData()
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="broadcastData"></param>
		public BroadcastData(BroadcastData broadcastData)
			: this()
		{
			Type = broadcastData.Type;
			HostSession = broadcastData.HostSession;
			Data = broadcastData.Data;
		}

		#region Methods

		public void SetData<T>(object data)
		{
			SetData(typeof(T), data);
		}

		public void SetData(Type type, object data)
		{
			Type = data == null ? type : data.GetType();
			Data = data;
		}

		/// <summary>
		/// Serialize this instance to a string.
		/// </summary>
		/// <returns></returns>
		public string Serialize()
		{
			return JsonConvert.SerializeObject(this);
		}

		#endregion
	}
}
