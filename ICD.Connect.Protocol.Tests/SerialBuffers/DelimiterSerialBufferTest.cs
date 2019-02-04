using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Protocol.SerialBuffers;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Tests.SerialBuffers
{
	[TestFixture]
	public sealed class DelimiterSerialBufferTest
	{
		[Test, UsedImplicitly]
		public void CompletedStringEventTest()
		{
			List<StringEventArgs> receivedEvents = new List<StringEventArgs>();
			DelimiterSerialBuffer buffer = new DelimiterSerialBuffer('\n');

			buffer.OnCompletedSerial += (sender, e) => receivedEvents.Add(e);

			buffer.Enqueue("Some");
			buffer.Enqueue(" multiline\n");
			buffer.Enqueue("\ntest data");
			buffer.Enqueue(".\n");

			Assert.AreEqual(2, receivedEvents.Count);
			Assert.AreEqual("Some multiline", receivedEvents[0].Data);
			Assert.AreEqual("test data.", receivedEvents[1].Data);

			receivedEvents.Clear();

			buffer.Enqueue("a\nb\nc\n");

			Assert.AreEqual(3, receivedEvents.Count);
			Assert.AreEqual("a", receivedEvents[0].Data);
			Assert.AreEqual("b", receivedEvents[1].Data);
			Assert.AreEqual("c", receivedEvents[2].Data);
		}
	}
}
