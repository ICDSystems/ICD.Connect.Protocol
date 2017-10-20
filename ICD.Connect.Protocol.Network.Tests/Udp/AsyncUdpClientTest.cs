using System.Linq;
using ICD.Common.Utils;
using ICD.Connect.Protocol.Network.Udp;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Network.Tests.Udp
{
	[TestFixture]
	public sealed class AsyncUdpClientTest
	{
		[Test]
		public void AddressTest()
		{
			using (AsyncUdpClient client = new AsyncUdpClient())
			{
				Assert.AreEqual(AsyncUdpClient.ACCEPT_ALL, client.Address);

				client.Address = "127.0.0.1";
				Assert.AreEqual("127.0.0.1", client.Address);

				client.Address = IcdEnvironment.NetworkAddresses.First();
				Assert.AreEqual("127.0.0.1", client.Address);
			}
		}

		[TestCase((ushort)0)]
		[TestCase((ushort)10)]
		public void PortTest(ushort port)
		{
			using (AsyncUdpClient client = new AsyncUdpClient {Port = port})
			{
				Assert.AreEqual(port, client.Port);
			}
		}

		[TestCase(10000)]
		public void BufferSizeTest(int bufferSize)
		{
			using (AsyncUdpClient client = new AsyncUdpClient {BufferSize = bufferSize})
			{
				Assert.AreEqual(bufferSize, client.BufferSize);
			}
		}

		[Test]
		public void ConnectTest()
		{
			Assert.Inconclusive();
		}

		[Test]
		public void DisconnectTest()
		{
			Assert.Inconclusive();
		}

		[Test]
		public void DisposeTest()
		{
			using (AsyncUdpClient client = new AsyncUdpClient())
			{
				client.Connect();
				Assert.DoesNotThrow(() => client.Dispose());
			}
		}

		[Test]
		public void SendToAddressTest()
		{
			Assert.Inconclusive();
		}
	}
}
