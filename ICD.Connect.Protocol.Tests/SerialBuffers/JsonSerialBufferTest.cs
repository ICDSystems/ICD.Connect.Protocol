using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Protocol.SerialBuffers;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Tests.SerialBuffers
{
	[TestFixture]
	public sealed class JsonSerialBufferTest
	{
		private const string EXAMPLE_JSON = "{"
		                                    + "\"status\": 200,"
		                                    + "\"message\": \"GET successful\","
		                                    + "\"data\": {"
		                                    + "\"key\": \"/v1.0/OnScreenText/\","
		                                    + "\"value\": ["
		                                    + "\"Language\","
		                                    + "\"Location\","
		                                    + "\"MeetingRoomName\","
		                                    + "\"ShowMeetingRoomInfo\","
		                                    + "\"ShowNetworkInfo\","
		                                    + "\"SupportedLanguages\","
		                                    + "\"WelcomeMessage\""
		                                    + "]"
		                                    + "}"
		                                    + "}";

		[Test, UsedImplicitly]
		public void CompletedStringEventTest()
		{
			List<StringEventArgs> receivedEvents = new List<StringEventArgs>();
			JsonSerialBuffer buffer = new JsonSerialBuffer();

			buffer.OnCompletedSerial += (sender, e) => receivedEvents.Add(e);

			buffer.Enqueue(EXAMPLE_JSON);
			Assert.AreEqual(1, receivedEvents.Count);
			Assert.AreEqual(EXAMPLE_JSON, receivedEvents[0].Data);
		}
	}
}