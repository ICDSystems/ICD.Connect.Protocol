using System.Collections.Generic;
using System.Linq;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.Ports;
using Newtonsoft.Json;

namespace ICD.Connect.Protocol.Crosspoints.Advertisements
{
	public sealed class Advertisement : ISerialData
	{
		private readonly HostInfo m_Source;
		private readonly CrosspointInfo[] m_Controls;
		private readonly CrosspointInfo[] m_Equipment;
		private readonly eAdvertisementType m_AdvertisementType;

		#region Properties

		/// <summary>
		/// The source of this advertisement.
		/// </summary>
		public HostInfo Source { get { return m_Source; } }

		/// <summary>
		/// The control crosspoints that are available.
		/// </summary>
		public IEnumerable<CrosspointInfo> Controls { get { return m_Controls.ToArray(); } }

		/// <summary>
		/// The equipment crosspoints that are available.
		/// </summary>
		public IEnumerable<CrosspointInfo> Equipment { get { return m_Equipment.ToArray(); } }

		/// <summary>
		/// The type of advertisement this is
		/// </summary>
		public eAdvertisementType AdvertisementType { get { return m_AdvertisementType; }}

		#endregion

		public Advertisement(HostInfo source, IEnumerable<CrosspointInfo> controls, IEnumerable<CrosspointInfo> equipment, eAdvertisementType advertisementType)
		{
			m_Source = source;
			m_Controls = controls.ToArray();
			m_Equipment = equipment.ToArray();
			m_AdvertisementType = advertisementType;
		}

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
			return JsonConvert.SerializeObject(this, Formatting.None,
			                                   new JsonSerializerSettings
			                                   {
				                                   DefaultValueHandling = DefaultValueHandling.Ignore
			                                   });
		}

		#endregion
	}

	public enum eAdvertisementType
		{
			Localhost,
			Multicast,
			Broadcast,
			Directed,
			DirectedRemove,
			Mesh
		}
}
