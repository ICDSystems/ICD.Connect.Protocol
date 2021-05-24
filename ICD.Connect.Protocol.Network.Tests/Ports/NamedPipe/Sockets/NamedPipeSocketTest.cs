using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Security.Principal;
using ICD.Common.Utils;
using ICD.Connect.Protocol.Network.Ports.NamedPipe.Sockets;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Network.Tests.Ports.NamedPipe.Sockets
{
	[TestFixture]
	public sealed class NamedPipeSocketTest
	{
		[Test]
		public void Test()
		{
			List<byte[]> clientDataReceived = new List<byte[]>();
			List<byte[]> serverDataReceived = new List<byte[]>();
			List<bool> clientConnectedChanged = new List<bool>();
			List<bool> serverConnectedChanged = new List<bool>();

			string pipeName = Guid.NewGuid().ToString();

			// Build the server
			NamedPipeServerStream serverStream =
				new NamedPipeServerStream(pipeName,
				                          PipeDirection.InOut,
				                          1,
				                          PipeTransmissionMode.Byte,
				                          PipeOptions.Asynchronous);

			using ServerNamedPipeSocket server = new ServerNamedPipeSocket(serverStream, pipeName);
			server.OnDataReceived += (sender, args) => serverDataReceived.Add(args.Data);
			server.OnIsConnectedChanged += (sender, args) => serverConnectedChanged.Add(args.Data);

			Assert.AreEqual(false, server.IsConnected);
			Assert.AreEqual(pipeName, server.PipeName);
			Assert.AreEqual(serverStream, server.Stream);

			// Build the client
			NamedPipeClientStream clientStream =
				new NamedPipeClientStream(".",
				                          pipeName,
				                          PipeDirection.InOut,
				                          PipeOptions.Asynchronous,
				                          TokenImpersonationLevel.Impersonation);

			using ClientNamedPipeSocket client = new ClientNamedPipeSocket(clientStream, ".", pipeName);
			client.OnDataReceived += (sender, args) => clientDataReceived.Add(args.Data);
			client.OnIsConnectedChanged += (sender, args) => clientConnectedChanged.Add(args.Data);

			Assert.AreEqual(false, client.IsConnected);
			Assert.AreEqual(pipeName, client.PipeName);
			Assert.AreEqual(clientStream, client.Stream);

			// Start listening
			server.ConnectAsync();
			ThreadingUtils.Sleep(TimeSpan.FromSeconds(1));

			Assert.IsFalse(server.IsConnected);

			// Connect the client
			bool timeout = !client.ConnectAsync().Wait(TimeSpan.FromSeconds(5));

			Assert.IsFalse(timeout);
			Assert.IsTrue(client.IsConnected);
			Assert.IsTrue(server.IsConnected);
			Assert.AreEqual(1, clientConnectedChanged.Count);
			Assert.AreEqual(true, clientConnectedChanged[0]);
			Assert.AreEqual(1, serverConnectedChanged.Count);
			Assert.AreEqual(true, serverConnectedChanged[0]);

			// Send message from client to server
			client.SendAsync(new byte[] {1, 2, 3}).Wait(TimeSpan.FromSeconds(5));
			ThreadingUtils.Wait(() => serverDataReceived.Count > 0, TimeSpan.FromSeconds(5));

			Assert.AreEqual(1, serverDataReceived.Count);
			CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, serverDataReceived[0]);

			// Send message from server to client
			server.SendAsync(new byte[] {4, 5, 6}).Wait(TimeSpan.FromSeconds(5));
			ThreadingUtils.Wait(() => clientDataReceived.Count > 0, TimeSpan.FromSeconds(5));

			Assert.AreEqual(1, clientDataReceived.Count);
			CollectionAssert.AreEqual(new byte[] { 4, 5, 6 }, clientDataReceived[0]);

			// Disconnect the client
			client.Dispose();
			Assert.AreEqual(false, client.IsConnected);

			ThreadingUtils.Wait(() => !server.IsConnected, TimeSpan.FromSeconds(5));
			Assert.AreEqual(false, server.IsConnected);

			server.Dispose();
		}
	}
}
