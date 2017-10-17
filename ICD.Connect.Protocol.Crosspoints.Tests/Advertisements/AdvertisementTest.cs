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
				new CrosspointInfo(1, "Control 1", null, 0),
				new CrosspointInfo(2, "Control 2", null, 0),
				new CrosspointInfo(3, "Control 3", null, 0)
			};

			Advertisement advertisement = new Advertisement(new HostInfo(), controls, Enumerable.Empty<CrosspointInfo>(),
			                                                eAdvertisementType.Broadcast);

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
				new CrosspointInfo(1, "Equipment 1", null, 0),
				new CrosspointInfo(2, "Equipment 2", null, 0),
				new CrosspointInfo(3, "Equipment 3", null, 0)
			};

			Advertisement advertisement = new Advertisement(new HostInfo(), Enumerable.Empty<CrosspointInfo>(), equipment,
			                                                eAdvertisementType.Broadcast);

			Assert.AreEqual(3, advertisement.Equipment.Count());
			Assert.IsTrue(advertisement.Equipment.Contains(equipment[0]));
			Assert.IsTrue(advertisement.Equipment.Contains(equipment[1]));
			Assert.IsTrue(advertisement.Equipment.Contains(equipment[2]));
		}

		[Test, UsedImplicitly]
		public void DeserializeTest()
		{
			const string serialized = @"{
        ""Source"": {
                        ""Address"": null,
                        ""Port"": 0
        },
        ""Controls"": [
                {
                        ""Host"": {
                                ""Address"": null,
                                ""Port"": 0
                        },
                        ""Id"": 1,
                        ""Name"": ""Control 1""
                },
                {
                        ""Host"": {
                                ""Address"": null,
                                ""Port"": 0
                        },
                        ""Id"": 2,
                        ""Name"": ""Control 2""
                },
                {
                        ""Host"": {
                                ""Address"": null,
                                ""Port"": 0
                        },
                        ""Id"": 3,
                        ""Name"": ""Control 3""
                }
        ],
        ""Equipment"": [
                {
                        ""Host"": {
                                ""Address"": null,
                                ""Port"": 0
                        },
                        ""Id"": 1,
                        ""Name"": ""Equipment 1""
                },
                {
                        ""Host"": {
                                ""Address"": null,
                                ""Port"": 0
                        },
                        ""Id"": 2,
                        ""Name"": ""Equipment 2""
                },
                {
                        ""Host"": {
                                ""Address"": null,
                                ""Port"": 0
                        },
                        ""Id"": 3,
                        ""Name"": ""Equipment 3""
                }
        ]
}";
			CrosspointInfo[] controls =
			{
				new CrosspointInfo(1, "Control 1", null, 0),
				new CrosspointInfo(2, "Control 2", null, 0),
				new CrosspointInfo(3, "Control 3", null, 0)
			};

			CrosspointInfo[] equipment =
			{
				new CrosspointInfo(1, "Equipment 1", null, 0),
				new CrosspointInfo(2, "Equipment 2", null, 0),
				new CrosspointInfo(3, "Equipment 3", null, 0)
			};

			Advertisement a = new Advertisement(new HostInfo(), controls, equipment, eAdvertisementType.Broadcast);
			Advertisement b = Advertisement.Deserialize(serialized);

			Assert.IsTrue(a.Controls.SequenceEqual(b.Controls));
			Assert.IsTrue(a.Equipment.SequenceEqual(b.Equipment));
		}

		[Test, UsedImplicitly]
		public void SerializeTest()
		{
			CrosspointInfo[] controls =
			{
				new CrosspointInfo(1, "Control 1", null, 0),
				new CrosspointInfo(2, "Control 2", null, 0),
				new CrosspointInfo(3, "Control 3", null, 0)
			};

			CrosspointInfo[] equipment =
			{
				new CrosspointInfo(1, "Equipment 1", null, 0),
				new CrosspointInfo(2, "Equipment 2", null, 0),
				new CrosspointInfo(3, "Equipment 3", null, 0)
			};

			Advertisement a = new Advertisement(new HostInfo(), controls, equipment, eAdvertisementType.Broadcast);
			string serialized = a.Serialize();
			Advertisement b = Advertisement.Deserialize(serialized);

			Assert.IsTrue(a.Controls.SequenceEqual(b.Controls));
			Assert.IsTrue(a.Equipment.SequenceEqual(b.Equipment));
		}
	}
}
