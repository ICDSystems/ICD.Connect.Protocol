#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using ICD.Common.Properties;
using ICD.Connect.Protocol.Network.Attributes.Rpc;
using ICD.Connect.Protocol.Network.RemoteProcedure;
using NUnit.Framework;
using System.Collections.Generic;

namespace ICD.Connect.Protocol.Network.Tests.RemoteProcedure
{
	[TestFixture]
	public sealed class RpcTest
	{
		[Test, UsedImplicitly]
		public void SetPropertyRpcTest()
		{
			const string expected = "Test{}[];";
			TestClient client = new TestClient();

			Rpc rpc = Rpc.SetPropertyRpc(TestClient.TEST_DATA_KEY, expected);
			rpc.Execute(client);

			Assert.AreEqual(expected, client.TestData);

			rpc = Rpc.SetPropertyRpc(TestClient.FAIL_DATA_KEY, expected);
			Assert.Throws<KeyNotFoundException>(() => rpc.Execute(client));
		}

		[Test, UsedImplicitly]
		public void CallMethodRpcTest()
		{
			const string expectedStringParam1 = null;
			const int expectedIntParam1 = 1;
			const int expectedIntParam2 = 2;

			TestClient client = new TestClient();

			Rpc rpc = Rpc.CallMethodRpc(TestClient.SET_DATA_KEY, expectedStringParam1, expectedIntParam1);
			rpc.Execute(client);

			Assert.AreEqual(expectedStringParam1, client.MethodString1);
			Assert.AreEqual(expectedIntParam1, client.MethodInt2);

			rpc = Rpc.CallMethodRpc(TestClient.SET_DATA_KEY, expectedIntParam1, expectedIntParam2);
			rpc.Execute(client);
			Assert.AreEqual(expectedIntParam1, client.MethodInt1);
			Assert.AreEqual(expectedIntParam2, client.MethodInt2);
		}

		[Test, UsedImplicitly]
		public void Execute()
		{
			// Tests execute for method
			CallMethodRpcTest();

			// Tests execute for property
			SetPropertyRpcTest();
		}

		[Test, UsedImplicitly]
		public void SerializeTest()
		{
			Rpc rpc = Rpc.CallMethodRpc("TestKey", (string)null, 1, 0.7f);

			string json = rpc.Serialize();

			Assert.IsTrue(json.Contains("TestKey"));
			Assert.IsTrue(json.Contains("1"));
			Assert.IsTrue(json.Contains("0.7"));
		}

		[Test, UsedImplicitly]
		public void DeserializeTest()
		{
			const string json = @"{""t"":0,""k"":""SetData"",""p"":[{""t"":null,""i"":null},{""t"":""System.Int32"",""i"":1}]}";

			TestClient client = new TestClient();

			Rpc rpc = JsonConvert.DeserializeObject<Rpc>(json);
			rpc.Execute(client);

			Assert.AreEqual(null, client.MethodString1);
			Assert.AreEqual(1, client.MethodInt2);
		}

		private sealed class TestClient
		{
			public const string TEST_DATA_KEY = "TestData";
			public const string FAIL_DATA_KEY = "FailData";
			public const string SET_DATA_KEY = "SetData";

			[Rpc(TEST_DATA_KEY), UsedImplicitly]
			public string TestData { get; private set; }

			[Rpc(FAIL_DATA_KEY), UsedImplicitly]
			public string FailData { get { return null; } }

			public string MethodString1 { get; private set; }
			public int MethodInt1 { get; private set; }
			public int MethodInt2 { get; private set; }

			[Rpc(SET_DATA_KEY), UsedImplicitly]
			private void SetData(string param1, int param2)
			{
				MethodString1 = param1;
				MethodInt2 = param2;
			}

			[Rpc(SET_DATA_KEY), UsedImplicitly]
			private void SetData(int param1, int param2)
			{
				MethodInt1 = param1;
				MethodInt2 = param2;
			}
		}
	}
}
