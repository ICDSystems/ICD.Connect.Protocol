using ICD.Common.Utils;
using ICD.Connect.Protocol.Sigs;

namespace ICD.Connect.Protocol.Crosspoints.Crosspoints
{
	/// <summary>
	/// EquipmentCrosspoints typically represent a piece of hardware like
	/// a lighting system, or HVAC.
	/// </summary>
	public sealed class EquipmentCrosspoint : AbstractEquipmentCrosspoint
	{
		// We cache changed sigs to send to controls on connect.
		private readonly SigCache m_SigCache;
		private readonly SafeCriticalSection m_SigCacheSection;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="name"></param>
		public EquipmentCrosspoint(int id, string name)
			: base(id, name)
		{
			m_SigCache = new SigCache();
			m_SigCacheSection = new SafeCriticalSection();
		}

		#region Methods

		/// <summary>
		/// Override this method to handle data before it is sent to XP3.
		/// </summary>
		/// <param name="data"></param>
		protected override void PreSendInputData(CrosspointData data)
		{
			m_SigCacheSection.Enter();

			try
			{
				// Cache sigs to send to controls on connect
				if (data != null)
					m_SigCache.AddHighRemoveLow(data.GetSigs());
			}
			finally
			{
				m_SigCacheSection.Leave();
			}

			base.PreSendInputData(data);
		}

		#endregion

		#region Private Methods

		protected override CrosspointData GetConnectCrosspointData(int controlId)
		{
			m_SigCacheSection.Enter();

			try
			{
				return CrosspointData.EquipmentConnect(controlId, Id, m_SigCache);
			}
			finally
			{
				m_SigCacheSection.Leave();
			}
		}

		protected override string PrintSigs()
		{
			return PrintSigs(m_SigCache);
		}

		#endregion
	}
}
