#if SIMPLSHARP
using ICD.Common.Properties;
using ICD.Connect.Protocol.Crosspoints.Advertisements;

namespace ICD.Connect.Protocol.Crosspoints.SimplPlus
{
	public sealed class SimplPlusAdvertisementManagerControls
	{
		#region fields

		private int m_SystemId;

		private CrosspointSystem m_System;

		#endregion

		#region SPlus Methods

		[PublicAPI]
		public void SetSystemId(ushort systemId)
		{
			m_SystemId = systemId;
		}

		[PublicAPI]
		public void AddAdvertisementAddress(string address)
		{
			if (m_System == null)
				return;
			if (address == null)
				return;
			m_System.AdvertisementManager.AddAdvertisementAddress(address, eAdvertisementType.Directed);
		}

		[PublicAPI]
		public void RemoveAdvertisementAddress(string address)
		{
			if (m_System == null)
				return;
			if (address == null)
				return;
			m_System.AdvertisementManager.RemoveAdvertisementAddress(address);
		}

		[PublicAPI]
		public void InstantiateModule()
		{
			if (m_SystemId == 0)
				return;
			m_System = SimplPlusStaticCore.Xp3Core.GetOrCreateSystem(m_SystemId);
		}

		#endregion
	}
}

#endif
