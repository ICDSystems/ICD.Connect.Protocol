using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Network.Ports.NamedPipe;
using ICD.Connect.Protocol.Network.Servers;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Network.Tests.Ports.NamedPipe
{
	[TestFixture]
	public sealed class NamedPipeClientTest
	{
		[Test]
		public void Test()
		{
			List<bool> clientConnectionStateChanges = new List<bool>();
			List<string> clientSerialDataReceived = new List<string>();
			List<SocketStateEventArgs> serverOnSocketStateChanges = new List<SocketStateEventArgs>();
			List<DataReceiveEventArgs> serverOnDataReceives = new List<DataReceiveEventArgs>();

			string pipeName = Guid.NewGuid().ToString();

			// Build the client
			using NamedPipeClient client =
				new NamedPipeClient
				{
					PipeName = pipeName
				};
			client.OnConnectedStateChanged += (sender, args) => clientConnectionStateChanges.Add(args.Data);
			client.OnSerialDataReceived += (sender, args) => clientSerialDataReceived.Add(args.Data);

			// Build the server
			using NamedPipeServer server =
				new NamedPipeServer
				{
					PipeName = pipeName
				};
			server.OnSocketStateChange += (sender, args) => serverOnSocketStateChanges.Add(args);
			server.OnDataReceived += (sender, args) => serverOnDataReceives.Add(args);

			// Start the server
			Assert.AreEqual(false, server.Listening);
			server.Start();
			ThreadingUtils.Wait(() => server.Listening, TimeSpan.FromSeconds(5));
			Assert.AreEqual(true, server.Listening);

			// Connect the client
			Assert.AreEqual(false, client.IsConnected);
			client.Connect();
			ThreadingUtils.Wait(() => client.IsConnected && server.NumberOfClients > 0, TimeSpan.FromSeconds(5));
			Assert.AreEqual(true, client.IsConnected);

			Assert.AreEqual(1, clientConnectionStateChanges.Count);
			Assert.AreEqual(true, clientConnectionStateChanges[0]);
			Assert.AreEqual(1, serverOnSocketStateChanges.Count);
			Assert.AreEqual(1, serverOnSocketStateChanges[0].ClientId);
			Assert.AreEqual(SocketStateEventArgs.eSocketStatus.SocketStatusConnected, serverOnSocketStateChanges[0].SocketState);
			Assert.AreEqual(1, server.NumberOfClients);

			// Send data from the client to the server
			client.Send("ping");
			ThreadingUtils.Sleep(5);
			Assert.AreEqual(1, serverOnDataReceives.Count);
			Assert.AreEqual("ping", serverOnDataReceives[0].Data);
			Assert.AreEqual(1, serverOnDataReceives[0].ClientId);

			// Send data from the server to the client
			server.Send(1, "pong");
			ThreadingUtils.Sleep(5);
			Assert.AreEqual(1, clientSerialDataReceived.Count);
			Assert.AreEqual("pong", clientSerialDataReceived[0]);

			// Disconnect the client
			client.Disconnect();
			ThreadingUtils.Sleep(5);
			Assert.AreEqual(false, client.IsConnected);
			Assert.AreEqual(2, clientConnectionStateChanges.Count);
			Assert.AreEqual(false, clientConnectionStateChanges[1]);

			Assert.AreEqual(2, serverOnSocketStateChanges.Count);
			Assert.AreEqual(1, serverOnSocketStateChanges[1].ClientId);
			Assert.AreEqual(SocketStateEventArgs.eSocketStatus.SocketStatusNoConnect, serverOnSocketStateChanges[1].SocketState);
			Assert.AreEqual(0, server.NumberOfClients);
			Assert.AreEqual(true, server.Listening);
		}
	}
}
