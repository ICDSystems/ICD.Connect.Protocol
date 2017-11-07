using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Protocol.SerialBuffers;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Tests.SerialBuffers
{
	[TestFixture]
    public sealed class XSigSerialBufferTest
    {
	    [Test]
	    public void CompletedStringEventTest()
	    {
		    List<StringEventArgs> receivedEvents = new List<StringEventArgs>();
		    XSigSerialBuffer buffer = new XSigSerialBuffer();

		    buffer.OnCompletedSerial += (sender, e) => receivedEvents.Add(e);

		    buffer.Enqueue("\x0A");
		    Assert.AreEqual(0, receivedEvents.Count);

		    buffer.Enqueue("\x80\x0A");
		    Assert.AreEqual(1, receivedEvents.Count);
			Assert.AreEqual("\x80\x0A", receivedEvents[0].Data);

		    buffer.Enqueue("\xC0");
		    Assert.AreEqual(1, receivedEvents.Count);

		    buffer.Enqueue("\xC0\x09\x01\x69");
		    Assert.AreEqual(2, receivedEvents.Count);
		    Assert.AreEqual("\xC0\x09\x01\x69", receivedEvents[1].Data);
		}
	}
}
