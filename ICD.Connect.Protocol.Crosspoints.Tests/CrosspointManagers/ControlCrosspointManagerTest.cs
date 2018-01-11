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

		[Test]
		public void AutoReconnectTest()
		{
			Assert.Inconclusive();
		}

		[Test, UsedImplicitly]
		public void ConnectCrosspointTest()
		{
			// Build the managers
			using (Xp3 xp3 = new Xp3())
			{
				CrosspointSystem system = xp3.CreateSystem(1);
				ControlCrosspointManager controlManager = system.CreateControlCrosspointManager();
				EquipmentCrosspointManager equipmentManager = system.CreateEquipmentCrosspointManager();

				// Add some crosspoints
				ControlCrosspoint crosspoint = new ControlCrosspoint(1, "Control 1");
				controlManager.RegisterCrosspoint(crosspoint);

				Assert.AreEqual(eCrosspointStatus.EquipmentNotFound, controlManager.ConnectCrosspoint(crosspoint, 10));

				equipmentManager.RegisterCrosspoint(new EquipmentCrosspoint(10, "Equipment 10"));

				Assert.AreEqual(eCrosspointStatus.Connected, controlManager.ConnectCrosspoint(crosspoint, 10));

				ControlCrosspoint crosspoint2 = new ControlCrosspoint(2, "Control 2");
				Assert.AreEqual(eCrosspointStatus.ControlNotFound, controlManager.ConnectCrosspoint(crosspoint2, 10));
			}
		}

		[Test, UsedImplicitly]
		public void ConnectCrosspointByIdTest()
		{
			// Build the managers
			using (Xp3 xp3 = new Xp3())
			{
				CrosspointSystem system = xp3.CreateSystem(1);
				ControlCrosspointManager controlManager = system.CreateControlCrosspointManager();
				EquipmentCrosspointManager equipmentManager = system.CreateEquipmentCrosspointManager();

				// Add some crosspoints
				ControlCrosspoint crosspoint = new ControlCrosspoint(1, "Control 1");
				controlManager.RegisterCrosspoint(crosspoint);

				Assert.AreEqual(eCrosspointStatus.EquipmentNotFound, controlManager.ConnectCrosspoint(crosspoint.Id, 10));

				equipmentManager.RegisterCrosspoint(new EquipmentCrosspoint(10, "Equipment 10"));

				Assert.AreEqual(eCrosspointStatus.Connected, controlManager.ConnectCrosspoint(crosspoint.Id, 10));
				Assert.AreEqual(eCrosspointStatus.ControlNotFound, controlManager.ConnectCrosspoint(2, 10));
			}
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
