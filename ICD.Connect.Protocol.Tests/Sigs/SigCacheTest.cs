using System.Linq;
using ICD.Common.Properties;
using ICD.Connect.Protocol.Sigs;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Tests.Sigs
{
	[TestFixture, UsedImplicitly]
	public sealed class SigCacheTest
	{
		[Test, UsedImplicitly]
		public void ClearTest()
		{
			SigCache cache = new SigCache
			{
				new SigInfo(1, 0, false),
				new SigInfo(1, 1, false),
				new SigInfo(1, 1, false)
			};

			Assert.AreEqual(2, cache.Count);

			cache.Clear();

			Assert.AreEqual(0, cache.Count);
		}

		[Test, UsedImplicitly]
		public void AddsTest()
		{
			SigCache cache = new SigCache();

			cache.AddRange(new[]
			{
				new SigInfo(1, 0, false),
				new SigInfo(1, 0, false)
			});

			Assert.AreEqual(1, cache.Count);
		}

		[Test, UsedImplicitly]
		public void AddTest()
		{
			// ReSharper disable once UseObjectOrCollectionInitializer
			SigCache cache = new SigCache();

			cache.Add(new SigInfo(1, 0, false));
			cache.Add(new SigInfo(1, 0, false));

			Assert.AreEqual(1, cache.Count);
		}

		[Test, UsedImplicitly]
		public void RemoveSigTest()
		{
			SigCache cache = new SigCache();

			SigInfo a = new SigInfo(1, 0, false);
			SigInfo b = new SigInfo(1, 1, false);

			cache.Add(a);
			cache.Add(b);

			Assert.AreEqual(2, cache.Count);

			cache.Remove(a);

			Assert.AreEqual(1, cache.Count);
		}

		[Test, UsedImplicitly]
		public void GetSigsTest()
		{
			SigCache cache = new SigCache();

			SigInfo numbered = new SigInfo(1, 0, false);

			cache.Add(numbered);

			SigInfo[] sigs = cache.ToArray();

			Assert.AreEqual(1, sigs.Length);

			Assert.IsTrue(sigs[0] == numbered);
		}
	}
}
