using System.Linq;
using ICD.Common.Properties;
using ICD.Connect.Protocol.Crosspoints.Advertisements;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using ICD.Connect.Protocol.Ports;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Crosspoints.Tests.Advertisements
{
	[TestFixture, UsedImplicitly]
	public sealed class AdvertisementTest
	{
		[Test, UsedImplicitly]
		public void ControlsTest()
		{
			CrosspointInfo[] controls =
			{
				new CrosspointInfo(1, "Control 1", new HostInfo(null, 0)),
				new CrosspointInfo(2, "Control 2", new HostInfo(null, 0)),
				new CrosspointInfo(3, "Control 3", new HostInfo(null, 0))
			};

			Advertisement advertisement = new Advertisement
			{
				Source = new HostInfo(),
				Controls = controls,
				Equipment = new CrosspointInfo[0],
			    AdvertisementType = eAdvertisementType.Broadcast
			};

			Assert.AreEqual(3, advertisement.Controls.Count());
			Assert.IsTrue(advertisement.Controls.Contains(controls[0]));
			Assert.IsTrue(advertisement.Controls.Contains(controls[1]));
			Assert.IsTrue(advertisement.Controls.Contains(controls[2]));
		}

		[Test, UsedImplicitly]
		public void EquipmentTest()
		{
			CrosspointInfo[] equipment =
			{
				new CrosspointInfo(1, "Equipment 1", new HostInfo(null, 0)),
				new CrosspointInfo(2, "Equipment 2", new HostInfo(null, 0)),
				new CrosspointInfo(3, "Equipment 3", new HostInfo(null, 0))
			};

			Advertisement advertisement = new Advertisement
			{
				Source = new HostInfo(),
				Controls = new CrosspointInfo[0],
				Equipment = equipment,
				AdvertisementType = eAdvertisementType.Broadcast
			};

			Assert.AreEqual(3, advertisement.Equipment.Count());
			Assert.IsTrue(advertisement.Equipment.Contains(equipment[0]));
			Assert.IsTrue(advertisement.Equipment.Contains(equipment[1]));
			Assert.IsTrue(advertisement.Equipment.Contains(equipment[2]));
		}

		[Test, UsedImplicitly]
		public void DeserializeTest()
		{
			const string serialized = "{\"c\":[{\"i\":1,\"n\":\"Control 1\"},{\"i\":2,\"n\":\"Control 2\"},{\"i\":3,\"n\":\"Control 3\"}],\"e\":[{\"i\":1,\"n\":\"Equipment 1\"},{\"i\":2,\"n\":\"Equipment 2\"},{\"i\":3,\"n\":\"Equipment 3\"}],\"a\":2}";
			CrosspointInfo[] controls =
			{
				new CrosspointInfo(1, "Control 1", new HostInfo(null, 0)),
				new CrosspointInfo(2, "Control 2", new HostInfo(null, 0)),
				new CrosspointInfo(3, "Control 3", new HostInfo(null, 0))
			};

			CrosspointInfo[] equipment =
			{
				new CrosspointInfo(1, "Equipment 1", new HostInfo(null, 0)),
				new CrosspointInfo(2, "Equipment 2", new HostInfo(null, 0)),
				new CrosspointInfo(3, "Equipment 3", new HostInfo(null, 0))
			};

			Advertisement a = new Advertisement
			{
				Source = new HostInfo(),
				Controls = controls,
				Equipment = equipment,
				AdvertisementType = eAdvertisementType.Broadcast
			};

			Advertisement b = Advertisement.Deserialize(serialized);

			Assert.IsTrue(a.Controls.SequenceEqual(b.Controls));
			Assert.IsTrue(a.Equipment.SequenceEqual(b.Equipment));
		}

		[Test, UsedImplicitly]
		public void SerializeTest()
		{
			CrosspointInfo[] controls =
			{
				new CrosspointInfo(1, "Control 1", new HostInfo(null, 0)),
				new CrosspointInfo(2, "Control 2", new HostInfo(null, 0)),
				new CrosspointInfo(3, "Control 3", new HostInfo(null, 0))
			};

			CrosspointInfo[] equipment =
			{
				new CrosspointInfo(1, "Equipment 1", new HostInfo(null, 0)),
				new CrosspointInfo(2, "Equipment 2", new HostInfo(null, 0)),
				new CrosspointInfo(3, "Equipment 3", new HostInfo(null, 0))
			};

			Advertisement a = new Advertisement
			{
				Source = new HostInfo(),
				Controls = controls,
				Equipment = equipment,
				AdvertisementType = eAdvertisementType.Broadcast
			};
			string serialized = a.Serialize();
			Advertisement b = Advertisement.Deserialize(serialized);

			Assert.IsTrue(a.Controls.SequenceEqual(b.Controls));
			Assert.IsTrue(a.Equipment.SequenceEqual(b.Equipment));
		}
	}
}
