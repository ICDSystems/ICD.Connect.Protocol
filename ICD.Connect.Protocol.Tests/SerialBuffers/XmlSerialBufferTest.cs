using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Protocol.SerialBuffers;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Tests.SerialBuffers
{
	[TestFixture]
	public sealed class XmlSerialBufferTest
	{
		[Test, UsedImplicitly]
		public void CompletedStringEventTest()
		{
			List<StringEventArgs> receivedEvents = new List<StringEventArgs>();
			XmlSerialBuffer buffer = new XmlSerialBuffer();

			buffer.OnCompletedSerial += (sender, e) => receivedEvents.Add(e);

			buffer.Enqueue(@"<Test>");
			Assert.AreEqual(0, receivedEvents.Count);

			buffer.Enqueue(@"</Test>");
			Assert.AreEqual(1, receivedEvents.Count);

			Assert.AreEqual(@"<Test></Test>", receivedEvents[0].Data);
		}
	}
}