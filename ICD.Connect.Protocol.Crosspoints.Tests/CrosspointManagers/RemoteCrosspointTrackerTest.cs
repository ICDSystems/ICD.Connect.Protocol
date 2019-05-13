using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.Crosspoints.CrosspointManagers;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using ICD.Connect.Protocol.Ports;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Crosspoints.Tests.CrosspointManagers
{
	[TestFixture, UsedImplicitly]
	public sealed class RemoteCrosspointTrackerTest
	{
		[Test, UsedImplicitly]
		public void CountTest()
		{
			RemoteCrosspointTracker tracker = new RemoteCrosspointTracker();

			Assert.AreEqual(0, tracker.Count);

			tracker.AddCrosspointInfo(new CrosspointInfo(1, "Crosspoint 1", new HostInfo(null, 0)));
			tracker.AddCrosspointInfo(new CrosspointInfo(1, "Crosspoint 1", new HostInfo(null, 0)));
			tracker.AddCrosspointInfo(new CrosspointInfo(2, "Crosspoint 2", new HostInfo(null, 0)));

			Assert.AreEqual(2, tracker.Count);
		}

		[UsedImplicitly]
		[TestCase(1)]
		public void AddCrosspointInfoTest(int id)
		{
			CrosspointInfo info = new CrosspointInfo(id, null, new HostInfo(null, 0));

			RemoteCrosspointTracker tracker = new RemoteCrosspointTracker();
			tracker.AddCrosspointInfo(info);

			Assert.AreEqual(info, tracker.GetCrosspointInfo(id));
		}

		[Test, UsedImplicitly]
		public void AddCrosspointInfoSequenceTest()
		{
			CrosspointInfo[] infos =
			{
				new CrosspointInfo(1, "Crosspoint 1", new HostInfo(null, 0)),
				new CrosspointInfo(1, "Crosspoint 1", new HostInfo(null, 0)),
				new CrosspointInfo(2, "Crosspoint 2", new HostInfo(null, 0))
			};

			RemoteCrosspointTracker tracker = new RemoteCrosspointTracker();
			tracker.AddCrosspointInfo(infos);

			Assert.IsTrue(tracker.ContainsCrosspointInfo(1));
			Assert.IsTrue(tracker.ContainsCrosspointInfo(2));
			Assert.AreEqual(2, tracker.Count);
		}

		[UsedImplicitly]
		[TestCase(1)]
		public void RemoveCrosspointInfoTest(int id)
		{
			RemoteCrosspointTracker tracker = new RemoteCrosspointTracker();
			tracker.AddCrosspointInfo(new CrosspointInfo(id, null, new HostInfo(null, 0)));

			Assert.AreEqual(1, tracker.Count);
			tracker.RemoveCrosspointInfo(id);
			Assert.AreEqual(0, tracker.Count);
		}

		[Test, UsedImplicitly]
		public void GetCrosspointInfoTest()
		{
			CrosspointInfo[] a =
			{
				new CrosspointInfo(1, "Crosspoint 1", new HostInfo(null, 0)),
				new CrosspointInfo(1, "Crosspoint 1", new HostInfo(null, 0)),
				new CrosspointInfo(2, "Crosspoint 2", new HostInfo(null, 0))
			};

			RemoteCrosspointTracker tracker = new RemoteCrosspointTracker();
			tracker.AddCrosspointInfo(a);

			CrosspointInfo[] b = tracker.GetCrosspointInfo().ToArray();

			Assert.IsTrue(a.Skip(1).ScrambledEquals(b));
		}

		[UsedImplicitly]
		[TestCase(1)]
		public void GetCrosspointInfoByIdTest(int id)
		{
			CrosspointInfo info = new CrosspointInfo(id, null, new HostInfo(null, 0));

			RemoteCrosspointTracker tracker = new RemoteCrosspointTracker();
			tracker.AddCrosspointInfo(info);

			Assert.AreEqual(info, tracker.GetCrosspointInfo(id));
		}

		[UsedImplicitly]
		[TestCase(1)]
		public void TryGetCrosspointInfoTest(int id)
		{
			RemoteCrosspointTracker tracker = new RemoteCrosspointTracker();

			CrosspointInfo info;
			Assert.IsFalse(tracker.TryGetCrosspointInfo(id, out info));

			CrosspointInfo expected = new CrosspointInfo(id, null, new HostInfo(null, 0));
			tracker.AddCrosspointInfo(expected);

			Assert.IsTrue(tracker.TryGetCrosspointInfo(id, out info));
			Assert.AreEqual(expected, info);
		}

		[UsedImplicitly]
		[TestCase(1)]
		public void ContainsCrosspointInfoTest(int id)
		{
			RemoteCrosspointTracker tracker = new RemoteCrosspointTracker();

			Assert.IsFalse(tracker.ContainsCrosspointInfo(id));

			tracker.AddCrosspointInfo(new CrosspointInfo(id, null, new HostInfo(null, 0)));

			Assert.IsTrue(tracker.ContainsCrosspointInfo(id));
		}
	}
}
