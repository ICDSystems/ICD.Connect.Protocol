using System.Linq;
using ICD.Common.Properties;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Crosspoints.Tests.Crosspoints
{
	[TestFixture, UsedImplicitly]
	public sealed class EquipmentCrosspointTest : AbstractCrosspointTest
	{
		protected override AbstractCrosspoint InstantiateCrosspoint(int id, string name)
		{
			return new EquipmentCrosspoint(id, name);
		}

		[UsedImplicitly]
		[TestCase(10)]
		public void InitializeTest(int controlId)
		{
			EquipmentCrosspoint crosspoint = InstantiateCrosspoint(1, null) as EquipmentCrosspoint;

			Assert.IsFalse(crosspoint.ControlCrosspoints.Contains(controlId));

			crosspoint.Initialize(controlId);
			Assert.IsTrue(crosspoint.ControlCrosspoints.Contains(controlId));
		}

		[UsedImplicitly]
		[TestCase(10)]
		public void DeinitializeTest(int controlId)
		{
			EquipmentCrosspoint crosspoint = InstantiateCrosspoint(1, null) as EquipmentCrosspoint;

			Assert.IsFalse(crosspoint.ControlCrosspoints.Contains(controlId));

			crosspoint.Initialize(controlId);
			Assert.IsTrue(crosspoint.ControlCrosspoints.Contains(controlId));

			crosspoint.Deinitialize(controlId);
			Assert.IsFalse(crosspoint.ControlCrosspoints.Contains(controlId));
		}
	}
}
