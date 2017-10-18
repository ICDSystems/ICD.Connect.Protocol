using ICD.Common.Properties;
using ICD.Connect.Protocol.Crosspoints.CrosspointManagers;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Crosspoints.Tests.CrosspointManagers
{
	[TestFixture, UsedImplicitly]
	public sealed class EquipmentCrosspointManagerTest : AbstractCrosspointManagerTest<EquipmentCrosspointManager, IEquipmentCrosspoint>
	{
		protected override EquipmentCrosspointManager InstantiateManager(int systemId)
		{
			return new EquipmentCrosspointManager(systemId);
		}

		protected override IEquipmentCrosspoint InstantiateCrosspoint(int id)
		{
			return new EquipmentCrosspoint(id, id.ToString());
		}
	}
}
