using ICD.Common.Properties;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Crosspoints.Tests.Crosspoints
{
	[TestFixture, UsedImplicitly]
	public sealed class ControlCrosspointTest : AbstractCrosspointTest
	{
		protected override AbstractCrosspoint InstantiateCrosspoint(int id, string name)
		{
			return new ControlCrosspoint(id, name);
		}

		[UsedImplicitly]
		[TestCase(100)]
		public void InitializeTest(int equipmentId)
		{
			ControlCrosspoint crosspoint = InstantiateCrosspoint(1, null) as ControlCrosspoint;

			// No connect callback
			Assert.IsFalse(crosspoint.Initialize(equipmentId));

			// Successful initialize
			int[] requestConnectCount = {0};
			int requestConnectEquipmentId = -1;
			int[] requestDisconnectCount = {0};

			crosspoint.RequestConnectCallback = (sender, id) =>
			                                    {
				                                    requestConnectCount[0]++;
				                                    requestConnectEquipmentId = id;
				                                    return eCrosspointStatus.Connected;
			                                    };

			crosspoint.RequestDisconnectCallback = sender =>
			                                       {
				                                       requestDisconnectCount[0]++;
				                                       return eCrosspointStatus.Idle;
			                                       };

			Assert.IsTrue(crosspoint.Initialize(equipmentId));
			Assert.AreEqual(0, requestDisconnectCount[0]);
			Assert.AreEqual(1, requestConnectCount[0]);
			Assert.AreEqual(equipmentId, requestConnectEquipmentId);
			Assert.AreEqual(equipmentId, crosspoint.EquipmentCrosspoint);

			// Initialize the same equipment again
			requestConnectCount[0] = 0;
			requestDisconnectCount[0] = 0;

			Assert.IsFalse(crosspoint.Initialize(equipmentId));
			Assert.AreEqual(0, requestDisconnectCount[0]);
			Assert.AreEqual(0, requestConnectCount[0]);
			Assert.AreEqual(equipmentId, crosspoint.EquipmentCrosspoint);

			// Deinit
			requestConnectCount[0] = 0;
			requestConnectEquipmentId = -1;
			requestDisconnectCount[0] = 0;

			Assert.IsTrue(crosspoint.Initialize(Xp3Utils.NULL_EQUIPMENT));
			Assert.AreEqual(1, requestDisconnectCount[0]);
			Assert.AreEqual(0, requestConnectCount[0]);
			Assert.AreEqual(Xp3Utils.NULL_EQUIPMENT, crosspoint.EquipmentCrosspoint);

			// Fail init
			requestConnectCount[0] = 0;
			requestConnectEquipmentId = -1;
			requestDisconnectCount[0] = 0;

			crosspoint.RequestConnectCallback = (sender, id) =>
			                                    {
				                                    requestConnectCount[0]++;
				                                    requestConnectEquipmentId = id;
				                                    return eCrosspointStatus.ConnectFailed;
			                                    };

			Assert.IsFalse(crosspoint.Initialize(equipmentId));
			Assert.AreEqual(0, requestDisconnectCount[0]);
			Assert.AreEqual(1, requestConnectCount[0]);
			Assert.AreEqual(equipmentId, requestConnectEquipmentId);
			Assert.AreEqual(Xp3Utils.NULL_EQUIPMENT, crosspoint.EquipmentCrosspoint);

			/*
			// Fail deinit
			requestConnectCount = 0;
			requestConnectEquipmentId = -1;
			requestDisconnectCount = 0;

			crosspoint.RequestConnectCallback = (sender, id) =>
			{
				requestConnectCount++;
				requestConnectEquipmentId = id;
				return true;
			};

			crosspoint.RequestDisconnectCallback = sender =>
			{
				requestDisconnectCount++;
				return true;
			};

			Assert.IsTrue(crosspoint.Initialize(equipmentId));
			Assert.AreEqual(1, requestDisconnectCount);
			Assert.AreEqual(1, requestConnectCount);
			Assert.AreEqual(equipmentId, requestConnectEquipmentId);
			Assert.AreEqual(equipmentId, crosspoint.EquipmentCrosspoint);
			 */

			Assert.Inconclusive();
		}

		[Test, UsedImplicitly]
		public void DeinitializeTest()
		{
			/*
			EquipmentCrosspoint = Xp3Utils.NULL_EQUIPMENT;

			return RequestDisconnectCallback != null && RequestDisconnectCallback(this);
			 */
			Assert.Inconclusive();
		}
	}
}
