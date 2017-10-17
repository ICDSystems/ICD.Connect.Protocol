using System;
using ICD.Common.Properties;
using ICD.Connect.Protocol.Crosspoints;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using NUnit.Framework;

namespace ICD.SimplSharp.XP3.Tests.Crosspoints
{
	[TestFixture, UsedImplicitly]
	public abstract class AbstractCrosspointTest
	{
		protected abstract AbstractCrosspoint InstantiateCrosspoint(int id, string name);

		[UsedImplicitly]
		[TestCase(100)]
		public void IdTest(int id)
		{
			AbstractCrosspoint crosspoint = InstantiateCrosspoint(id, null);
			Assert.AreEqual(id, crosspoint.Id);
		}

		[Test, UsedImplicitly]
		public void NullIdTest()
		{
			Assert.Throws<ArgumentException>(() => InstantiateCrosspoint(0, null));
		}

		[UsedImplicitly]
		[TestCase(null)]
		[TestCase("")]
		[TestCase("test")]
		public void NameTest(string name)
		{
			AbstractCrosspoint crosspoint = InstantiateCrosspoint(1, name);
			Assert.AreEqual(name, crosspoint.Name);
		}

		[Test, UsedImplicitly]
		public void SendNullOutputDataTest()
		{
			AbstractCrosspoint crosspoint = InstantiateCrosspoint(1, null);
			bool eventRaised = false;

			crosspoint.OnSendOutputData += (sender, data) => eventRaised = true;

			Assert.Throws<ArgumentNullException>(() => crosspoint.SendOutputData(null));
			Assert.IsFalse(eventRaised);
		}

		[Test, UsedImplicitly]
		public void SendNullInputDataTest()
		{
			AbstractCrosspoint crosspoint = InstantiateCrosspoint(1, null);
			bool eventRaised = false;

			crosspoint.OnSendInputData += (sender, data) => eventRaised = true;

			Assert.Throws<ArgumentNullException>(() => crosspoint.SendInputData(null));
			Assert.IsFalse(eventRaised);
		}

		[Test, UsedImplicitly]
		public void SendOutputDataTest()
		{
			AbstractCrosspoint crosspoint = InstantiateCrosspoint(1, null);
			bool eventRaised = false;

			crosspoint.OnSendOutputData += (sender, data) => eventRaised = true;

			Assert.DoesNotThrow(() => crosspoint.SendOutputData(new CrosspointData()));
			Assert.IsTrue(eventRaised);
		}

		[Test, UsedImplicitly]
		public void SendInputDataTest()
		{
			AbstractCrosspoint crosspoint = InstantiateCrosspoint(1, null);
			bool eventRaised = false;

			crosspoint.OnSendInputData += (sender, data) => eventRaised = true;

			Assert.DoesNotThrow(() => crosspoint.SendInputData(new CrosspointData()));
			Assert.IsTrue(eventRaised);
		}
	}
}
