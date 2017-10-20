using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Connect.Protocol.Crosspoints.CrosspointManagers;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Crosspoints.Tests.CrosspointManagers
{
	[TestFixture]
	public abstract class AbstractCrosspointManagerTest<TManager, TCrosspoint>
		where TManager : AbstractCrosspointManager<TCrosspoint>
		where TCrosspoint : class, ICrosspoint
	{
		protected abstract TManager InstantiateManager(int systemId);

		protected abstract TCrosspoint InstantiateCrosspoint(int id);

		[UsedImplicitly]
		[TestCase(1)]
		public void SystemIdTest(int systemId)
		{
			using (TManager manager = InstantiateManager(systemId))
			{
				Assert.AreEqual(systemId, manager.SystemId);
			}
		}

		[Test, UsedImplicitly]
		public void RemoteCrosspointsTest()
		{
			Assert.Inconclusive();
		}

		[Test, UsedImplicitly]
		public void ClearCrosspointsTest()
		{
			Assert.Inconclusive();
		}

		[Test, UsedImplicitly]
		public void GetCrosspointIdsTest()
		{
			using (TManager manager = InstantiateManager(1))
			{
				TCrosspoint a = InstantiateCrosspoint(1);
				TCrosspoint b = InstantiateCrosspoint(2);
				TCrosspoint c = InstantiateCrosspoint(3);

				manager.RegisterCrosspoint(a);
				manager.RegisterCrosspoint(b);
				manager.RegisterCrosspoint(c);

				int[] crosspoints = manager.GetCrosspointIds().ToArray();

				Assert.AreEqual(3, crosspoints.Length);
				Assert.IsTrue(crosspoints.Contains(1));
				Assert.IsTrue(crosspoints.Contains(2));
				Assert.IsTrue(crosspoints.Contains(3));
			}
		}

		[Test]
		public void GetCrosspointsTest()
		{
			using (TManager manager = InstantiateManager(1))
			{
				TCrosspoint a = InstantiateCrosspoint(1);
				TCrosspoint b = InstantiateCrosspoint(2);
				TCrosspoint c = InstantiateCrosspoint(3);

				manager.RegisterCrosspoint(a);
				manager.RegisterCrosspoint(b);
				manager.RegisterCrosspoint(c);

				TCrosspoint[] crosspoints = manager.GetCrosspoints().ToArray();

				Assert.AreEqual(3, crosspoints.Length);
				Assert.IsTrue(crosspoints.Contains(a));
				Assert.IsTrue(crosspoints.Contains(b));
				Assert.IsTrue(crosspoints.Contains(c));
			}
		}

		[UsedImplicitly]
		[TestCase(1)]
		public void GetCrosspointTest(int id)
		{
			using (TManager manager = InstantiateManager(1))
			{
				TCrosspoint a = InstantiateCrosspoint(id);

				Assert.Throws<KeyNotFoundException>(() => manager.GetCrosspoint(id));

				manager.RegisterCrosspoint(a);
				TCrosspoint b = manager.GetCrosspoint(id);

				Assert.AreEqual(a, b);
			}
		}

		[UsedImplicitly]
		[TestCase(1)]
		public void TryGetCrosspointTest(int id)
		{
			using (TManager manager = InstantiateManager(1))
			{
				TCrosspoint a = InstantiateCrosspoint(id);

				TCrosspoint b;
				Assert.IsFalse(manager.TryGetCrosspoint(id, out b));

				manager.RegisterCrosspoint(a);
				Assert.IsTrue(manager.TryGetCrosspoint(id, out b));

				Assert.AreEqual(a, b);
			}
		}

		[UsedImplicitly]
		[TestCase(1)]
		public void RegisterCrosspointTest(int id)
		{
			List<ICrosspoint> registered = new List<ICrosspoint>();

			using (TManager manager = InstantiateManager(1))
			{
				manager.OnCrosspointRegistered += (sender, cp) => registered.Add(cp);

				TCrosspoint crosspoint = InstantiateCrosspoint(id);

				Assert.DoesNotThrow(() => manager.RegisterCrosspoint(crosspoint));
				Assert.Throws<ArgumentException>(() => manager.RegisterCrosspoint(crosspoint));
				Assert.AreEqual(1, registered.Count);
				Assert.IsTrue(registered.Contains(crosspoint));
			}
		}

		[UsedImplicitly]
		[TestCase(1)]
		public void UnregisterCrosspointTest(int id)
		{
			List<ICrosspoint> unregistered = new List<ICrosspoint>();

			using (TManager manager = InstantiateManager(1))
			{
				manager.OnCrosspointUnregistered += (sender, cp) => unregistered.Add(cp);

				TCrosspoint crosspoint = InstantiateCrosspoint(id);
				manager.RegisterCrosspoint(crosspoint);
				manager.UnregisterCrosspoint(crosspoint);
				manager.UnregisterCrosspoint(crosspoint);

				Assert.AreEqual(1, unregistered.Count);
				Assert.IsTrue(unregistered.Contains(crosspoint));
			}
		}

		[Test, UsedImplicitly]
		public void GetHostInfoTest()
		{
			Assert.Inconclusive();
		}
	}
}
