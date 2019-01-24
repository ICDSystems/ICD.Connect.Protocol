using System;
using ICD.Common.Properties;
using ICD.Connect.Protocol.Sigs;
using Newtonsoft.Json;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Tests.Sigs
{
	[TestFixture]
	public sealed class SigInfoTest
	{
		[Test, UsedImplicitly]
		public void TypeTest()
		{
			SigInfo sig = new SigInfo(0, 0, 0);
			Assert.AreEqual(eSigType.Analog, sig.Type);

			sig = new SigInfo(0, 0, true);
			Assert.AreEqual(eSigType.Digital, sig.Type);

			sig = new SigInfo(0, 0, "test");
			Assert.AreEqual(eSigType.Serial, sig.Type);
		}

		[UsedImplicitly]
		[TestCase((ushort)1, "test", (uint)10)]
		public void NumberTest(ushort smartObject, string name, uint number)
		{
			SigInfo sig = new SigInfo(number, name, smartObject, 0);
			Assert.AreEqual(number, sig.Number);
		}

		[UsedImplicitly]
		[TestCase((ushort)1, "test", (uint)10)]
		public void NameTest(ushort smartObject, string name, uint number)
		{
			SigInfo sig = new SigInfo(number, name, smartObject, 0);
			Assert.AreEqual(name, sig.Name);
		}

		[UsedImplicitly]
		[TestCase((ushort)1, "test", (uint)10)]
		public void SmartObjectTest(ushort smartObject, string name, uint number)
		{
			SigInfo sig = new SigInfo(number, name, smartObject, 0);
			Assert.AreEqual(smartObject, sig.SmartObject);
		}

		[UsedImplicitly]
		[TestCase((ushort)1, "test", (uint)10, "test")]
		public void GetStringValueTest(ushort smartObject, string name, uint number, string value)
		{
			SigInfo sig1 = new SigInfo(number, name, smartObject, 0);
			Assert.Throws<InvalidOperationException>(() => sig1.GetStringValue());

			SigInfo sig2 = new SigInfo(number, name, smartObject, value);
			Assert.AreEqual(value, sig2.GetStringValue());
		}

		[UsedImplicitly]
		[TestCase((ushort)1, "test", (uint)10, (ushort)100)]
		public void GetUShortValueTest(ushort smartObject, string name, uint number, ushort value)
		{
			SigInfo sig1 = new SigInfo(number, name, smartObject, null);
			Assert.Throws<InvalidOperationException>(() => sig1.GetUShortValue());

			SigInfo sig2 = new SigInfo(number, name, smartObject, value);
			Assert.AreEqual(value, sig2.GetUShortValue());
		}

		[UsedImplicitly]
		[TestCase((ushort)1, "test", (uint)10, true)]
		public void GetBoolValueTest(ushort smartObject, string name, uint number, bool value)
		{
			SigInfo sig1 = new SigInfo(number, name, smartObject, 0);
			Assert.Throws<InvalidOperationException>(() => sig1.GetBoolValue());

			SigInfo sig2 = new SigInfo(number, name, smartObject, value);
			Assert.AreEqual(value, sig2.GetBoolValue());
		}

		[Test, UsedImplicitly]
		public void SerializeTest()
		{
			SigInfo sig = new SigInfo(10, "test", 1, "serial");
			string serial = JsonConvert.SerializeObject(sig);

			SigInfo result = JsonConvert.DeserializeObject<SigInfo>(serial);

			Assert.AreEqual(sig, result);
		}

		[Test, UsedImplicitly]
		public void DeserializeTest()
		{
			const string serialized =
			@"{
				""T"": 3,
				""No"": 10,
				""Na"": ""test"",
				""SO"": 1,
				""V"": ""serial""
			}";

			SigInfo expected = new SigInfo(10, "test", 1, "serial");
			SigInfo sig = JsonConvert.DeserializeObject<SigInfo>(serialized);

			Assert.AreEqual(expected, sig);
		}
	}
}
