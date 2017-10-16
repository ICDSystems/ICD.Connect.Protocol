using System.Linq;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Crosspoints.Tests
{
	[TestFixture]
	public sealed class Xp3UtilsTest
	{
		[Test]
		public void GetAdvertisementMulticastPortsTest()
		{
			ushort[] ports = Xp3Utils.GetAdvertisementMulticastPorts(1).ToArray();
			Assert.AreEqual(10, ports.Length);
			Assert.AreEqual(10, ports.Distinct().Count());
		}

		[TestCase((uint)1, 1, (ushort)30010)]
		[TestCase((uint)1, 2, (ushort)30020)]
		[TestCase((uint)2, 1, (ushort)30011)]
		public void GetPortForSlotAndSystemTest(uint programNumber, int systemId, ushort expected)
		{
			Assert.AreEqual(expected, Xp3Utils.GetPortForSlotAndSystem(programNumber, systemId));
		}

		[TestCase(1, (ushort)30010)]
		public void GetPortForSystemTest(int systemId, ushort expected)
		{
			Assert.AreEqual(expected, Xp3Utils.GetPortForSystem(systemId));
		}
	}
}
