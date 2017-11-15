using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.SerialQueues;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Tests.SerialQueues
{
	[TestFixture]
	public sealed class SerialQueueTest
	{
		[Test, UsedImplicitly]
		public void ResponseEventTest()
		{
			List<SerialResponseEventArgs> responses = new List<SerialResponseEventArgs>();

			DelimiterSerialBuffer buffer = new DelimiterSerialBuffer('\n');
			ComPortPlus port = new ComPortPlus(1);
			SerialQueue queue = new SerialQueue();
			queue.SetPort(port);
			queue.SetBuffer(buffer);

			queue.OnSerialResponse += (sender, e) => responses.Add(e);

			// Queue some Tx
			queue.Enqueue("Ping");
			queue.Enqueue("Ping2");

			// Fake some Rx
			port.Receive("Pong\n");
			port.Receive("Pong2\n");

			Assert.AreEqual(2, responses.Count);

			Assert.AreEqual("Ping", responses[0].Data.Serialize());
			Assert.AreEqual("Pong", responses[0].Response);

			Assert.AreEqual("Ping2", responses[1].Data.Serialize());
			Assert.AreEqual("Pong2", responses[1].Response);
		}

		[Test, UsedImplicitly]
		public void NullCommandTest()
		{
			List<SerialResponseEventArgs> responses = new List<SerialResponseEventArgs>();

			DelimiterSerialBuffer buffer = new DelimiterSerialBuffer('\n');
			ComPortPlus port = new ComPortPlus(1);
			SerialQueue queue = new SerialQueue();
			queue.SetPort(port);
			queue.SetBuffer(buffer);

			queue.OnSerialResponse += (sender, e) => responses.Add(e);

			// Fake some Rx
			port.Receive("Pong\n");
			port.Receive("Pong2\n");

			Assert.AreEqual(2, responses.Count);

			Assert.AreEqual(null, responses[0].Data);
			Assert.AreEqual("Pong", responses[0].Response);

			Assert.AreEqual(null, responses[1].Data);
			Assert.AreEqual("Pong2", responses[1].Response);
		}

		[Test, UsedImplicitly]
		public void TimeoutTest()
		{
			List<SerialDataEventArgs> responses = new List<SerialDataEventArgs>();

			DelimiterSerialBuffer buffer = new DelimiterSerialBuffer('\n');
			ComPortPlus port = new ComPortPlus(1);
			SerialQueue queue = new SerialQueue { Timeout = 100 };
			queue.SetPort(port);
			queue.SetBuffer(buffer);

			queue.OnTimeout += (sender, e) => responses.Add(e);

			// Queue some Tx
			queue.Enqueue("Ping\n");

			Assert.IsTrue(ThreadingUtils.Wait(() => responses.Count == 1, 200));
		}

		[Test, UsedImplicitly]
		public void DisconnectedTest()
		{
			Assert.Inconclusive();
			/*
			List<SerialResponseEventArgs> responses = new List<SerialResponseEventArgs>();

			DelimiterSerialBuffer buffer = new DelimiterSerialBuffer('\n');
			ComPortPlus port = new ComPortPlus(1);
			SerialQueue queue = new SerialQueue { Timeout = 100 };
			queue.SetPort(port);
			queue.SetBuffer(buffer);

			queue.OnSerialResponse += (sender, e) => responses.Add(e);

			// Queue some Tx
			queue.Enqueue("Ping\n");

			Assert.AreEqual(0, queue.DisconnectedTime);

			ThreadingUtils.Sleep(200);

			Assert.AreEqual(100, queue.DisconnectedTime, 10);
			*/
		}
	}
}
