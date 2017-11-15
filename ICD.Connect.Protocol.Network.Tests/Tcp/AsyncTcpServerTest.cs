using ICD.Connect.Protocol.Network.Tcp;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Network.Tests.Tcp
{
	[TestFixture]
	public sealed class AsyncTcpServerTest
	{
		[Test]
		public void DataReceivedEventTest()
		{
			Assert.Inconclusive();
		}

		[Test]
		public void SocketStateChangeEventTest()
		{
			Assert.Inconclusive();
		}

		[TestCase("test")]
		public void AddressToAcceptConnectionFromTest(string address)
		{
			AsyncTcpServer server = new AsyncTcpServer
			{
				AddressToAcceptConnectionFrom = address
			};

			Assert.AreEqual(address, server.AddressToAcceptConnectionFrom);
		}

		[TestCase((ushort)3000)]
		public void PortTest(ushort port)
		{
			AsyncTcpServer server = new AsyncTcpServer
			{
				Port = port
			};

			Assert.AreEqual(port, server.Port);
		}

		[Test]
		public void ActiveTest()
		{
			Assert.Inconclusive();
		}

		[TestCase(10, 10)]
		[TestCase(-1, 0)]
		[TestCase(65, 64)]
		public void MaxNumberOfClientsTest(int clients, int expected)
		{
			AsyncTcpServer server = new AsyncTcpServer
			{
				MaxNumberOfClients = clients
			};

			Assert.AreEqual(expected, server.MaxNumberOfClients);
		}

		[Test]
		public void RestartTest()
		{
			Assert.Inconclusive();
		}

		[Test]
		public void GetClientsTest()
		{
			Assert.Inconclusive();
		}

		[Test]
		public void StartTest()
		{
			Assert.Inconclusive();
		}

		[Test]
		public void StopTest()
		{
			Assert.Inconclusive();
		}

		[Test]
		public void SendTest()
		{
			Assert.Inconclusive();
		}

		[Test]
		public void SendClientIdTest()
		{
			Assert.Inconclusive();
		}

		[Test]
		public void GetClientInfoTest()
		{
			Assert.Inconclusive();
		}

		[Test]
		public void ClientConnectedTest()
		{
			Assert.Inconclusive();
		}
	}
}
