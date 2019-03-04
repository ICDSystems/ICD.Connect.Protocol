using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Connect.Protocol.Sigs;

namespace ICD.Connect.Protocol.Crosspoints.Crosspoints
{
	public delegate IEnumerable<SigInfo> GetInitialSigsDelegate(int controlId);

	public sealed class NonCachingEquipmentCrosspoint : AbstractEquipmentCrosspoint
	{
		[PublicAPI]
		public GetInitialSigsDelegate GetInitialSigs { get; set; }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="name"></param>
		public NonCachingEquipmentCrosspoint(int id, string name) : base(id, name)
		{
		}

		protected override CrosspointData GetConnectCrosspointData(int controlId)
		{
			GetInitialSigsDelegate handler = GetInitialSigs;
			if (handler == null)
				return base.GetConnectCrosspointData(controlId);

			return CrosspointData.EquipmentConnect(controlId, Id, handler(controlId));
		}

		protected override string PrintSigs()
		{
			return null;
		}
	}
}