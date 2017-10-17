using ICD.Common.Properties;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using ICD.Connect.Protocol.Ports;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Crosspoints.Tests.Crosspoints
{
	[TestFixture, UsedImplicitly]
	public sealed class CrosspointInfoTest
	{
		[UsedImplicitly]
		[TestCase(null, (ushort)0)]
		[TestCase("127.0.0.1", (ushort)22)]
		public void HostTest(string address, ushort port)
		{
			CrosspointInfo info = new CrosspointInfo(0, null, address, port);
			Assert.AreEqual(address, info.Host.Address);
			Assert.AreEqual(port, info.Host.Port);

			HostInfo hostInfo = new HostInfo(address, port);
			info = new CrosspointInfo(0, null, hostInfo);
			Assert.AreEqual(hostInfo, info.Host);
		}

		[UsedImplicitly]
		[TestCase(0)]
		[TestCase(1000)]
		public void IdTest(int id)
		{
			CrosspointInfo info = new CrosspointInfo(id, null, null, 0);
			Assert.AreEqual(id, info.Id);
		}

		[UsedImplicitly]
		[TestCase(null)]
		[TestCase("")]
		[TestCase("Test")]
		public void NameTest(string name)
		{
			CrosspointInfo info = new CrosspointInfo(0, name, null, 0);
			Assert.AreEqual(name, info.Name);
		}

		[UsedImplicitly]
		[TestCase(0, null, null, (ushort)0)]
		[TestCase(1, "test", "localhost", (ushort)22)]
		public void EqualsTest(int id, string name, string address, ushort port)
		{
			CrosspointInfo a = new CrosspointInfo(id, name, address, port);
			CrosspointInfo b = new CrosspointInfo(id, name, address, port);

			Assert.AreEqual(a, b);
		}

		[UsedImplicitly]
		[Test]
		public void NotEqualTest()
		{
			CrosspointInfo a = new CrosspointInfo(1, "test", "localhost", 22);
			CrosspointInfo b = new CrosspointInfo(0, null, null, 0);

			Assert.AreNotEqual(a, b);
		}
	}
}
