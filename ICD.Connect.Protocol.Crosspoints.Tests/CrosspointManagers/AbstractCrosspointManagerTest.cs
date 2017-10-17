using ICD.Common.Properties;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Crosspoints.Tests.CrosspointManagers
{
	[TestFixture, UsedImplicitly]
	public abstract class AbstractCrosspointManagerTest
	{
		[Test, UsedImplicitly]
		public void CrosspointsChangedFeedbackTest()
		{
			Assert.Inconclusive();
		}

		[UsedImplicitly]
		[TestCase(1)]
		public void SystemIdTest(int systemId)
		{
			Assert.Inconclusive();
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
			Assert.Inconclusive();
		}

		[PublicAPI]
		public void GetCrosspointsTest()
		{
			Assert.Inconclusive();
		}

		[UsedImplicitly]
		[TestCase(1)]
		public void GetCrosspointTest(int id)
		{
			Assert.Inconclusive();
		}

		[UsedImplicitly]
		[TestCase(1)]
		public void TryGetCrosspointTest(int id)
		{
			Assert.Inconclusive();
		}

		[UsedImplicitly]
		[TestCase(1)]
		public void RegisterCrosspointTest(int id)
		{
			Assert.Inconclusive();
		}

		[UsedImplicitly]
		[TestCase(1)]
		public void UnregisterCrosspointTest(int id)
		{
			Assert.Inconclusive();
		}

		[Test, UsedImplicitly]
		public void GetHostInfoTest()
		{
			Assert.Inconclusive();
		}
	}
}
