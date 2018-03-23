using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using NUnit.Framework;
using ICD.Connect.Protocol.FeedbackDebounce;

namespace ICD.Connect.Protocol.Tests.FeedbackDebounce
{
	[TestFixture]
	public sealed class FeedbackDebounceTest
	{
		[Test]
		public static void DebounceSimpleTest()
		{
			List<GenericEventArgs<bool>> feedback = new List<GenericEventArgs<bool>>();

			FeedbackDebounce<bool> bounce = new FeedbackDebounce<bool>();

			bounce.OnValue += (sender, args) => { feedback.Add(args); };

			bounce.Enqueue(true);
			bounce.Enqueue(true);
			ThreadingUtils.Wait(() => feedback.Count > 0, 1000);

			bounce.Enqueue(false);
			bounce.Enqueue(false);
			ThreadingUtils.Wait(() => feedback.Count > 1, 1000);

			Assert.AreEqual(2, feedback.Count);
			Assert.AreEqual(true, feedback[0].Data);
			Assert.AreEqual(false, feedback[1].Data);
		}

		[Test]
		public static void DebounceTest()
		{
			List<GenericEventArgs<bool>> feedback = new List<GenericEventArgs<bool>>();

			FeedbackDebounce<bool> bounce = new FeedbackDebounce<bool>();

			bounce.OnValue += (sender, args) =>
							  {
								  feedback.Add(args);

								  // Simulate some process time
								  ThreadingUtils.Sleep(100);
							  };

			bool toggle = false;

			for (int index = 0; index < 20; index++)
			{
				toggle = !toggle;
				bounce.Enqueue(toggle);
				ThreadingUtils.Sleep(10);
			}

			Assert.AreEqual(2, feedback.Count, 1);
			Assert.AreEqual(true, feedback[0].Data);
			Assert.AreEqual(false, feedback[1].Data);
		}
	}
}
