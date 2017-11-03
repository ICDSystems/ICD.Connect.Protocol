using ICD.Common.Utils;
using ICD.Connect.Protocol.XSig;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Tests.XSig
{
	[TestFixture]
    public sealed class DigitalXSigTest
    {
	    [TestCase(true, (ushort)10, "\x80\x0A")]
	    [TestCase(false, (ushort)14, "\xA0\x0E")]
		public void DataTest(bool value, ushort index, string expected)
	    {
		    DigitalXsig sig = new DigitalXsig(value, index);
			Assert.AreEqual(StringUtils.ToBytes(expected), sig.Data);
	    }

		[TestCase(true, (ushort)10)]
		public void ValueTest(bool value, ushort index)
		{
			DigitalXsig sig = new DigitalXsig(value, index);
			Assert.AreEqual(value, sig.Value);
		}

	    [TestCase(true, (ushort)10)]
		public void Index(bool value, ushort index)
	    {
		    DigitalXsig sig = new DigitalXsig(value, index);
		    Assert.AreEqual(index, sig.Index);
	    }

	    [TestCase("\x80\x0A")]
	    public void DataFromDataTest(string data)
	    {
		    DigitalXsig sig = new DigitalXsig(StringUtils.ToBytes(data));
		    Assert.AreEqual(data, StringUtils.ToString(sig.Data));
	    }

	    [TestCase("\x80\x0A", true)]
	    public void ValueFromDataTest(string data, bool value)
	    {
		    DigitalXsig sig = new DigitalXsig(StringUtils.ToBytes(data));
		    Assert.AreEqual(value, sig.Value);
	    }

	    [TestCase("\x80\x0A", (ushort)10)]
	    public void IndexFromData(string data, ushort index)
	    {
		    DigitalXsig sig = new DigitalXsig(StringUtils.ToBytes(data));
		    Assert.AreEqual(index, sig.Index);
	    }

		[TestCase("\x00", false)]
		[TestCase("\x80\x0A", true)]
		public void IsDigital(string data, bool expected)
		{
			byte[] bytes = StringUtils.ToBytes(data);
			Assert.AreEqual(expected, DigitalXsig.IsDigital(bytes));
	    }
	}
}
