using System.Linq;
using ICD.Common.Utils;
using ICD.Connect.Protocol.Ports;
using Newtonsoft.Json;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Tests.Ports
{
	[TestFixture]
	public sealed class HostInfoTest
	{
		[TestCase("127.0.0.1")]
		public void AddressTest(string address)
		{
			HostInfo info = new HostInfo(address, 0);
			Assert.AreEqual(address, info.Address);
		}

		[TestCase((ushort)3000)]
		public void PortTest(ushort port)
		{
			HostInfo info = new HostInfo(null, port);
			Assert.AreEqual(port, info.Port);
		}

		[Test]
		public void AddressOrLocalhostTest()
		{
			string address = IcdEnvironment.NetworkAddresses.First(a => a != "127.0.0.1");
			HostInfo info = new HostInfo(address, 0);

			Assert.AreEqual("127.0.0.1", info.AddressOrLocalhost);

			address = "test";
			info = new HostInfo(address, 0);

			Assert.AreEqual(address, info.AddressOrLocalhost);
		}

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

		[TestCase(true, "localhost", (ushort)100, "localhost:100")]
		[TestCase(false, null, (ushort)0, "test")]
		public void TryParseTest(bool expected, string expectedHostname, ushort expectedPort, string data)
		{
			HostInfo output;
			bool result = HostInfo.TryParse(data, out output);

			Assert.AreEqual(expected, result);
			Assert.AreEqual(expectedHostname, output.Address);
			Assert.AreEqual(expectedPort, output.Port);
		}

		[Test]
		public void JsonSerializationTest()
		{
			HostInfo host = new HostInfo("localhost", 100);
			string data = JsonConvert.SerializeObject(host);

			Assert.AreEqual("\"localhost:100\"", data);
		}

		[Test]
		public void JsonDeserializationTest()
		{
			string data = "\"localhost:100\"";
			HostInfo host = JsonConvert.DeserializeObject<HostInfo>(data);

			Assert.AreEqual(new HostInfo("localhost", 100), host);
		}
	}
}
