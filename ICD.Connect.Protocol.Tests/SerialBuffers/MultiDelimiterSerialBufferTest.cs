using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Protocol.SerialBuffers;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Tests.SerialBuffers
{
	[TestFixture]
	public sealed class MultiDelimiterSerialBufferTest
	{
		[Test, UsedImplicitly]
		public void CompletedStringEventTest()
		{
			List<StringEventArgs> receivedEvents = new List<StringEventArgs>();
			MultiDelimiterSerialBuffer buffer = new MultiDelimiterSerialBuffer('\n', '\r');

			buffer.OnCompletedSerial += (sender, e) => receivedEvents.Add(e);

			buffer.Enqueue("Some\n");
			buffer.Enqueue("multiline\r");
			buffer.Enqueue("test\rdata");
			buffer.Enqueue(".\n");

			Assert.AreEqual(4, receivedEvents.Count);
			Assert.AreEqual("Some", receivedEvents[0].Data);
			Assert.AreEqual("multiline", receivedEvents[1].Data);
			Assert.AreEqual("test", receivedEvents[2].Data);
			Assert.AreEqual("data.", receivedEvents[3].Data);
		}
	}
}
