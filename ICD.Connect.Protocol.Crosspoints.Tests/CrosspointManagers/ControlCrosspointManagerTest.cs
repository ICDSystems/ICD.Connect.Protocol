using ICD.Common.Properties;
using ICD.Connect.Protocol.Crosspoints.CrosspointManagers;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Crosspoints.Tests.CrosspointManagers
{
	[TestFixture, UsedImplicitly]
	public sealed class ControlCrosspointManagerTest : AbstractCrosspointManagerTest<ControlCrosspointManager, IControlCrosspoint>
	{
		protected override ControlCrosspointManager InstantiateManager(int systemId)
		{
			return new ControlCrosspointManager(systemId);
		}

		protected override IControlCrosspoint InstantiateCrosspoint(int id)
		{
			return new ControlCrosspoint(id, id.ToString());
		}

		[Test, UsedImplicitly]
		public void ConnectCrosspointTest()
		{
			Assert.Inconclusive();
		}

		[Test, UsedImplicitly]
		public void ConnectCrosspointByIdTest()
		{
			Assert.Inconclusive();
		}

		[Test, UsedImplicitly]
		public void DisconnectCrosspointTest()
		{
			Assert.Inconclusive();
		}

		[Test, UsedImplicitly]
		public void DisconnectCrosspointByIdTest()
		{
			Assert.Inconclusive();
		}

		[Test, UsedImplicitly]
		public void TryGetEquipmentInfoForControl()
		{
			Assert.Inconclusive();
		}

		[Test, UsedImplicitly]
		public void TryGetEquipmentForControl()
		{
			Assert.Inconclusive();
		}
	}
}
