using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.Json;
using ICD.Connect.Protocol.Sigs;
using Newtonsoft.Json;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Crosspoints.Tests.Parsing
{
	[TestFixture, UsedImplicitly]
	public sealed class CrosspointDataTest
	{
		[UsedImplicitly]
		[TestCase(1, 1)]
		public void ControlConnectTest(int controlId, int equipmentId)
		{
			CrosspointData data = CrosspointData.ControlConnect(controlId, equipmentId);

			Assert.AreEqual(CrosspointData.eMessageType.ControlConnect, data.MessageType);
			Assert.AreEqual(controlId, data.GetControlIds().First());
			Assert.AreEqual(equipmentId, data.EquipmentId);
		}

		[UsedImplicitly]
		[TestCase(1, 1)]
		public void ControlDisconnectTest(int controlId, int equipmentId)
		{
			CrosspointData data = CrosspointData.ControlDisconnect(controlId, equipmentId);

			Assert.AreEqual(CrosspointData.eMessageType.ControlDisconnect, data.MessageType);
			Assert.AreEqual(controlId, data.GetControlIds().First());
			Assert.AreEqual(equipmentId, data.EquipmentId);
		}

		[Test, UsedImplicitly]
		public void ControlClearTest()
		{
			SigInfo a = new SigInfo(1, "test a", 10, "test");
			SigInfo b = new SigInfo(2, "test b", 10, null);
			SigInfo c = new SigInfo(3, "test c", 10, true);
			SigInfo d = new SigInfo(4, "test d", 10, false);
			SigInfo e = new SigInfo(5, "test e", 10, 10);
			SigInfo f = new SigInfo(6, "test f", 10, 0);

			SigInfo[] sigs = {a, b, c, d, e, f};

			CrosspointData data = CrosspointData.ControlClear(1, 2, sigs);

			Assert.AreEqual(CrosspointData.eMessageType.ControlClear, data.MessageType);
			Assert.AreEqual(1, data.GetControlIds().First());
			Assert.AreEqual(2, data.EquipmentId);
			
			SigInfo[] clearSigs = data.GetSigs().ToArray();

			Assert.AreEqual(6, clearSigs.Length);
			Assert.IsTrue(clearSigs.Any(s => s.Number == a.Number && s.Name == a.Name && s.SmartObject == a.SmartObject));
			Assert.IsTrue(clearSigs.Any(s => s.Number == b.Number && s.Name == b.Name && s.SmartObject == b.SmartObject));
			Assert.IsTrue(clearSigs.Any(s => s.Number == c.Number && s.Name == c.Name && s.SmartObject == c.SmartObject));
			Assert.IsTrue(clearSigs.Any(s => s.Number == d.Number && s.Name == d.Name && s.SmartObject == d.SmartObject));
			Assert.IsTrue(clearSigs.Any(s => s.Number == e.Number && s.Name == e.Name && s.SmartObject == e.SmartObject));
			Assert.IsTrue(clearSigs.Any(s => s.Number == f.Number && s.Name == f.Name && s.SmartObject == f.SmartObject));
		}

		[Test, UsedImplicitly]
		public void EquipmentConnectTest()
		{
			SigInfo a = new SigInfo(1, "test a", 10, "test");
			SigInfo b = new SigInfo(2, "test b", 10, null);
			SigInfo c = new SigInfo(3, "test c", 10, true);
			SigInfo d = new SigInfo(4, "test d", 10, false);
			SigInfo e = new SigInfo(5, "test e", 10, 10);
			SigInfo f = new SigInfo(6, "test f", 10, 0);

			SigInfo[] sigs = {a, b, c, d, e, f};

			CrosspointData data = CrosspointData.EquipmentConnect(1, 2, sigs);

			Assert.AreEqual(CrosspointData.eMessageType.EquipmentConnect, data.MessageType);
			Assert.AreEqual(1, data.GetControlIds().First());
			Assert.AreEqual(2, data.EquipmentId);

			Assert.AreEqual(3, data.GetSigs().Count());
			Assert.IsTrue(data.GetSigs().Any(s => s.Number == a.Number && s.Name == a.Name && s.SmartObject == a.SmartObject && s.HasValue()));
			Assert.IsTrue(data.GetSigs().Any(s => s.Number == c.Number && s.Name == c.Name && s.SmartObject == c.SmartObject && s.HasValue()));
			Assert.IsTrue(data.GetSigs().Any(s => s.Number == e.Number && s.Name == e.Name && s.SmartObject == e.SmartObject && s.HasValue()));
		}

		[UsedImplicitly]
		[TestCase(1, 1)]
		public void EquipmentDisconnectTest(int controlId, int equipmentId)
		{
			CrosspointData data = CrosspointData.EquipmentDisconnect(controlId, equipmentId);

			Assert.AreEqual(CrosspointData.eMessageType.EquipmentDisconnect, data.MessageType);
			Assert.AreEqual(controlId, data.GetControlIds().First());
			Assert.AreEqual(equipmentId, data.EquipmentId);
		}

		[Test, UsedImplicitly]
		public void PingTest()
		{
			CrosspointData data = CrosspointData.Ping(new[] {1}, 1, "test");

			Assert.AreEqual(CrosspointData.eMessageType.Ping, data.MessageType);
			Assert.AreEqual(1, data.GetControlIds().First());
			Assert.AreEqual(1, data.EquipmentId);

			Assert.AreEqual("test", JsonConvert.DeserializeObject<string>(data.GetJson().First()));
		}

		[Test, UsedImplicitly]
		public void PongTest()
		{
			CrosspointData data = CrosspointData.Pong(new[] {1}, 1, "test");

			Assert.AreEqual(CrosspointData.eMessageType.Pong, data.MessageType);
			Assert.AreEqual(1, data.GetControlIds().First());
			Assert.AreEqual(1, data.EquipmentId);

			Assert.AreEqual("test", JsonConvert.DeserializeObject<string>(data.GetJson().First()));
		}

		[Test, UsedImplicitly]
		public void SerializeTest()
		{
			SigInfo a = new SigInfo("test", 0, null);
			SigInfo b = new SigInfo(10, 1, false);
			SigInfo c = new SigInfo(10, "test", 0, false);

			CrosspointData data = CrosspointData.ControlConnect(1, 2);

			data.AddSig(a);
			data.AddSig(b);
			data.AddSig(c);
			data.AddJson("test");

			string json = data.Serialize();

			CrosspointData deserialized = CrosspointData.Deserialize(json);

			Assert.AreEqual(CrosspointData.eMessageType.ControlConnect, data.MessageType);

			Assert.AreEqual(1, data.GetControlIds().First());
			Assert.AreEqual(2, data.EquipmentId);

			Assert.IsTrue(deserialized.GetJson().Contains("test"));

			Assert.IsTrue(deserialized.GetSigs().Any(s => s.Name == a.Name && s.GetType() == a.GetType()));
			Assert.IsTrue(
			              deserialized.GetSigs()
			                          .Any(
			                               s =>
			                               s.SmartObject == b.SmartObject && s.Number == b.Number && s.GetType() == b.GetType()));
			Assert.IsTrue(deserialized.GetSigs().Any(s => s.Name == c.Name && s.Number == c.Number && s.GetType() == c.GetType()));
		}

		[Test, UsedImplicitly]
		public void DeserializeTest()
		{
			const string json = @"
{
        ""T"": ""ControlConnect"",
        ""EId"": 2,
        ""CIds"": [
                1
        ],
        ""J"": [
                ""test""
        ],
        ""S"": [
                {
                        ""T"": ""Serial"",
                        ""Na"": ""test""
                },
                {
                        ""T"": ""Analog"",
                        ""No"": 10,
                        ""SO"": 1
                },
                {
                        ""T"": ""Digital"",
                        ""No"": 10,
                        ""Na"": ""test""
                }
        ]
}";

			SigInfo a = new SigInfo(0, "test", 0, null);
			SigInfo b = new SigInfo(10, null, 1, 0);
			SigInfo c = new SigInfo(10, "test", 0, false);

			CrosspointData data = CrosspointData.Deserialize(json);

			Assert.AreEqual(CrosspointData.eMessageType.ControlConnect, data.MessageType);

			Assert.AreEqual(1, data.GetControlIds().First());
			Assert.AreEqual(2, data.EquipmentId);

			Assert.IsTrue(data.GetJson().Contains("test"));

			Assert.IsTrue(data.GetSigs().Any(s => s == a));
			Assert.IsTrue(data.GetSigs().Any(s => s == b));
			Assert.IsTrue(data.GetSigs().Any(s => s == c));
		}

		[Test, UsedImplicitly]
		public void GetControlIdsTest()
		{
			CrosspointData data = new CrosspointData();

			data.AddControlId(1);
			data.AddControlId(1);
			data.AddControlId(2);

			Assert.AreEqual(2, data.GetControlIds().Count());
			Assert.IsTrue(data.GetControlIds().Contains(1));
			Assert.IsTrue(data.GetControlIds().Contains(2));
		}

		[UsedImplicitly]
		[TestCase(1)]
		public void AddControlIdTest(int controlId)
		{
			CrosspointData data = new CrosspointData();

			Assert.IsFalse(data.GetControlIds().Contains(controlId));

			data.AddControlId(controlId);

			Assert.IsTrue(data.GetControlIds().Contains(controlId));
		}

		[UsedImplicitly]
		[TestCase("test")]
		public void AddJsonTest(string json)
		{
			CrosspointData data = new CrosspointData();

			Assert.AreEqual(0, data.GetJson().Count());

			data.AddJson(json);
			data.AddJson(json);

			Assert.AreEqual(1, data.GetJson().Count());
		}

		[UsedImplicitly]
		[TestCase("test")]
		public void RemoveJsonTest(string json)
		{
			CrosspointData data = new CrosspointData();

			data.AddJson(json);
			data.RemoveJson(json);

			Assert.AreEqual(0, data.GetJson().Count());
		}

		[UsedImplicitly]
		[TestCase("test")]
		public void GetJsonTest(string json)
		{
			CrosspointData data = new CrosspointData();

			data.AddJson(json);

			Assert.AreEqual(json, data.GetJson().First());
		}

		[UsedImplicitly]
		[TestCase((ushort)1, "test", (uint)10)]
		public void AddSigTest(ushort smartObject, string name, uint number)
		{
			SigInfo sigInfo = new SigInfo(number, name, smartObject, null);

			CrosspointData data = new CrosspointData();
			data.AddSig(sigInfo);

			Assert.IsTrue(data.GetSigs().Contains(sigInfo));
		}

		[Test, UsedImplicitly]
		public void AddSigsTest()
		{
			CrosspointData data = new CrosspointData();

			SigInfo a = new SigInfo(1, "test a", 10, "test");
			SigInfo b = new SigInfo(2, "test b", 10, null);
			SigInfo c = new SigInfo(3, "test c", 10, true);
			SigInfo d = new SigInfo(4, "test d", 10, false);
			SigInfo e = new SigInfo(5, "test e", 10, 10);
			SigInfo f = new SigInfo(6, "test f", 10, 0);

			data.AddSigs(new [] {a, b, c, d, e, f, a, b, c, d, e, f});

			Assert.AreEqual(6, data.GetSigs().Count());
		}

		[UsedImplicitly]
		[TestCase((ushort)1, "test", (uint)10)]
		public void RemoveSigTest(ushort smartObject, string name, uint number)
		{
			SigInfo sigInfo = new SigInfo(number, name, smartObject, null);

			CrosspointData data = new CrosspointData();
			data.AddSig(sigInfo);
			data.RemoveSig(sigInfo);

			Assert.IsFalse(data.GetSigs().Any());
		}

		[Test, UsedImplicitly]
		public void GetSigsTest()
		{
			SigInfo a = new SigInfo("test", 0, null);
			SigInfo b = new SigInfo(10, 1, 0);
			SigInfo c = new SigInfo(10, "test", 0, false);

			CrosspointData data = new CrosspointData();

			data.AddSig(a);
			data.AddSig(b);
			data.AddSig(c);

			Assert.IsTrue(data.GetSigs().Contains(a));
			Assert.IsTrue(data.GetSigs().Contains(b));
			Assert.IsTrue(data.GetSigs().Contains(c));
		}
	}
}
