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

			for (int i = 0; i < 20; i++)
			{
				bounce.Enqueue(true);
				bounce.Enqueue(false);
			}

			ThreadingUtils.Sleep(1 * 1000);

			Assert.AreEqual(2, feedback.Count);
			Assert.AreEqual(true, feedback[0].Data);
			Assert.AreEqual(false, feedback[1].Data);
		}
	}
}
