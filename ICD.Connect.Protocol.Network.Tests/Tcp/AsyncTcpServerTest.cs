using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Protocol.EventArguments;
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
			List<TcpReceiveEventArgs> feedback = new List<TcpReceiveEventArgs>();

			AsyncTcpServer server = new AsyncTcpServer
			{
				AddressToAcceptConnectionFrom = "localhost",
				Port = 12345,
				MaxNumberOfClients = 1
			};
			server.OnDataReceived += (sender, args) => feedback.Add(args);

			AsyncTcpClient client = new AsyncTcpClient
			{
				Address = "localhost",
				Port = 12345
			};

			server.Start();
			client.Connect();

			Assert.IsTrue(ThreadingUtils.Wait(() => client.IsConnected && server.GetClients().Any(), 500));

			uint clientId = server.GetClients().First();

			Assert.AreEqual(0, feedback.Count);

			client.Send("test");

			Assert.IsTrue(ThreadingUtils.Wait(() => feedback.Count == 1, 500));
			Assert.AreEqual(clientId, feedback[0].ClientId);
			Assert.AreEqual("test", feedback[0].Data);
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
