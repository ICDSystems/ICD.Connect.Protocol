using System.Linq;
using ICD.Common.Utils;
using ICD.Connect.Protocol.Ports;
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
		public void TryParseTest()
		{
			Assert.Inconclusive();
		}
	}
}
