using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Connect.Protocol.Network.Ports.Udp;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Network.Tests.Ports.Udp
{
	[TestFixture]
	public sealed class IcdUdpClientTest
	{
		[TestCase("127.0.0.1")]
		[TestCase("localhost")]
		[TestCase("test.com")]
		public void AddressTest(string address)
		{
			using (IcdUdpClient client = new IcdUdpClient())
			{
				Assert.AreEqual(IcdUdpClient.ACCEPT_ALL, client.Address);

				client.Address = address;
				Assert.AreEqual(address, client.Address);
			}
		}

		[TestCase((ushort)0)]
		[TestCase((ushort)10)]
		public void PortTest(ushort port)
		{
			using (IcdUdpClient client = new IcdUdpClient { Port = port})
			{
				Assert.AreEqual(port, client.Port);
			}
		}

		[Test]
		public void ConnectTest()
		{
			using (IcdUdpClient client = new IcdUdpClient())
			{
				List<bool> feedback = new List<bool>();

				client.OnConnectedStateChanged += (sender, args) => feedback.Add(args.Data);

				Assert.IsFalse(client.IsConnected);

				client.Connect();

				Assert.IsTrue(client.IsConnected);
				Assert.AreEqual(1, feedback.Count);
				Assert.IsTrue(feedback[0]);
			}
		}

		[Test]
		public void DisconnectTest()
		{
			using (IcdUdpClient client = new IcdUdpClient())
			{
				List<bool> feedback = new List<bool>();

				client.OnConnectedStateChanged += (sender, args) => feedback.Add(args.Data);

				Assert.IsFalse(client.IsConnected);

				client.Connect();

				Assert.IsTrue(client.IsConnected);
				Assert.AreEqual(1, feedback.Count);
				Assert.IsTrue(feedback[0]);

				client.Disconnect();

				Assert.IsFalse(client.IsConnected);
				Assert.AreEqual(2, feedback.Count);
				Assert.IsFalse(feedback[1]);
			}
		}

		[Test]
		public void DisposeTest()
		{
			using (IcdUdpClient client = new IcdUdpClient())
			{
				client.Connect();
				Assert.DoesNotThrow(() => client.Dispose());
			}
		}

		// Purposely doing at least 2 tests. I've seen some weird behaviour with
		// UDP sockets not being recyled properly between client instances.
		[TestCase("test1")]
		[TestCase("test2")]
		public void SendToAddressTest(string data)
		{
			using (IcdUdpClient client = new IcdUdpClient { Port = 12345 })
			{
				List<string> feedback = new List<string>();

				client.OnSerialDataReceived += (sender, args) => feedback.Add(args.Data);

				client.Connect();

				Assert.IsTrue(client.SendToAddress(data, "localhost", 12345));

				ThreadingUtils.Sleep(200);

				Assert.AreEqual(1, feedback.Count);
				Assert.AreEqual(data, feedback[0]);
			}
		}
	}
}
