using System;
using ICD.Connect.Protocol.Network.Broadcast;
using Newtonsoft.Json;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Network.Tests.Broadcast
{
	[TestFixture]
	public sealed class HostSessionInfoTest
	{
		[Test]
		public void EqualityTest()
		{
			Assert.Inconclusive();
		}

		[Test]
		public void InequalityTest()
		{
			Assert.Inconclusive();
		}

		[Test]
		public void EqualsTest()
		{
			Assert.Inconclusive();
		}

		[Test]
		public void ParseTest()
		{
			Assert.Inconclusive();
		}

		[Test]
		public void TryParseTest()
		{
			Assert.Inconclusive();
		}

		[Test]
		public void JsonSerializationTest()
		{
			HostSessionInfo info = new HostSessionInfo("localhost", 100, new Guid("0e4bddf8-c69b-4f89-96ef-51ebaa784de3"));
			string data = JsonConvert.SerializeObject(info);

			Assert.AreEqual("\"localhost:100:0e4bddf8-c69b-4f89-96ef-51ebaa784de3\"", data);
		}

		[Test]
		public void JsonDeserializationTest()
		{
			string data = "\"localhost:100:0e4bddf8-c69b-4f89-96ef-51ebaa784de3\"";
			HostSessionInfo host = JsonConvert.DeserializeObject<HostSessionInfo>(data);

			Assert.AreEqual(new HostSessionInfo("localhost", 100, new Guid("0e4bddf8-c69b-4f89-96ef-51ebaa784de3")), host);
		}
	}
}
