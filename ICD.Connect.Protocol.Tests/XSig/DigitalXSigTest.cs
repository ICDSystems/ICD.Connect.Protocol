using ICD.Common.Utils;
using ICD.Connect.Protocol.XSig;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Tests.XSig
{
	[TestFixture]
    public sealed class DigitalXSigTest
    {
	    [TestCase(true, (ushort)11, "\x80\x0A")]
	    [TestCase(false, (ushort)15, "\xA0\x0E")]
	    [TestCase(false, (ushort)1, "\xA0\x00")]
        [TestCase(true, (ushort)1, "\x80\x00")]
	    [TestCase(false, (ushort)1366, "\xAA\x55")]
	    [TestCase(true, (ushort)1366, "\x8A\x55")]
        [TestCase(false, (ushort)2731, "\xB5\x2A")]
	    [TestCase(true, (ushort)2731, "\x95\x2A")]
        [TestCase(false, (ushort)4096, "\xBF\x7F")]
	    [TestCase(true, (ushort)4096, "\x9F\x7F")]
        public void DataTest(bool value, ushort index, string expected)
	    {
		    DigitalXSig sig = new DigitalXSig(value, index);
			Assert.AreEqual(StringUtils.ToBytes(expected), sig.Data);
	    }

        [TestCase(true, (ushort)11, "\x80\x0A")]
        [TestCase(false, (ushort)15, "\xA0\x0E")]
        [TestCase(false, (ushort)1, "\xA0\x00")]
        [TestCase(true, (ushort)1, "\x80\x00")]
        [TestCase(false, (ushort)1366, "\xAA\x55")]
        [TestCase(true, (ushort)1366, "\x8A\x55")]
        [TestCase(false, (ushort)2731, "\xB5\x2A")]
        [TestCase(true, (ushort)2731, "\x95\x2A")]
        [TestCase(false, (ushort)4096, "\xBF\x7F")]
        [TestCase(true, (ushort)4096, "\x9F\x7F")]
        public void DataStringTest(bool value, ushort index, string expected)
        {
            DigitalXSig sig = new DigitalXSig(value, index);
            Assert.AreEqual(expected, sig.DataXSig);
        }

        [TestCase(true, (ushort)11)]
        [TestCase(false, (ushort)15)]
        [TestCase(false, (ushort)1)]
        [TestCase(true, (ushort)1)]
        [TestCase(false, (ushort)1366)]
        [TestCase(true, (ushort)1366)]
        [TestCase(false, (ushort)2731)]
        [TestCase(true, (ushort)2731)]
        [TestCase(false, (ushort)4096)]
        [TestCase(true, (ushort)4096)]
        public void ValueTest(bool value, ushort index)
		{
			DigitalXSig sig = new DigitalXSig(value, index);
			Assert.AreEqual(value, sig.Value);
		}

        [TestCase(true, (ushort)11)]
        [TestCase(false, (ushort)15)]
        [TestCase(false, (ushort)1)]
        [TestCase(true, (ushort)1)]
        [TestCase(false, (ushort)1366)]
        [TestCase(true, (ushort)1366)]
        [TestCase(false, (ushort)2731)]
        [TestCase(true, (ushort)2731)]
        [TestCase(false, (ushort)4096)]
        [TestCase(true, (ushort)4096)]
        public void IndexTest(bool value, ushort index)
	    {
		    DigitalXSig sig = new DigitalXSig(value, index);
		    Assert.AreEqual(index, sig.Index);
	    }

        [TestCase("\x80\x0A")]
        [TestCase("\xA0\x0E")]
        [TestCase("\xA0\x00")]
        [TestCase("\x80\x00")]
        [TestCase("\xAA\x55")]
        [TestCase("\x8A\x55")]
        [TestCase("\xB5\x2A")]
        [TestCase("\x95\x2A")]
        [TestCase("\xBF\x7F")]
        [TestCase("\x9F\x7F")]
        public void DataFromDataTest(string data)
	    {
		    DigitalXSig sig = new DigitalXSig(StringUtils.ToBytes(data));
		    Assert.AreEqual(data, StringUtils.ToString(sig.Data));
	    }

        [TestCase(true, "\x80\x0A")]
        [TestCase(false, "\xA0\x0E")]
        [TestCase(false, "\xA0\x00")]
        [TestCase(true, "\x80\x00")]
        [TestCase(false, "\xAA\x55")]
        [TestCase(true, "\x8A\x55")]
        [TestCase(false, "\xB5\x2A")]
        [TestCase(true, "\x95\x2A")]
        [TestCase(false, "\xBF\x7F")]
        [TestCase(true, "\x9F\x7F")]
        public void ValueFromDataTest(bool value, string data)
	    {
		    DigitalXSig sig = new DigitalXSig(StringUtils.ToBytes(data));
		    Assert.AreEqual(value, sig.Value);
	    }

        [TestCase((ushort)11, "\x80\x0A")]
        [TestCase((ushort)15, "\xA0\x0E")]
        [TestCase((ushort)1, "\xA0\x00")]
        [TestCase((ushort)1, "\x80\x00")]
        [TestCase((ushort)1366, "\xAA\x55")]
        [TestCase((ushort)1366, "\x8A\x55")]
        [TestCase((ushort)2731, "\xB5\x2A")]
        [TestCase((ushort)2731, "\x95\x2A")]
        [TestCase((ushort)4096, "\xBF\x7F")]
        [TestCase((ushort)4096, "\x9F\x7F")]
        public void IndexFromData(ushort index, string data)
	    {
		    DigitalXSig sig = new DigitalXSig(StringUtils.ToBytes(data));
		    Assert.AreEqual(index, sig.Index);
	    }

        [TestCase("\x80\x0A", true)]
        [TestCase("\xA0\x0E", true)]
        [TestCase("\xA0\x00", true)]
        [TestCase("\x80\x00", true)]
        [TestCase("\xAA\x55", true)]
        [TestCase("\x8A\x55", true)]
        [TestCase("\xB5\x2A", true)]
        [TestCase("\x95\x2A", true)]
        [TestCase("\xBF\x7F", true)]
        [TestCase("\x9F\x7F", true)]
        [TestCase("\xC0\x09\x01\x69", false)]
        [TestCase("\xC0\x00\x00\x00", false)]
        [TestCase("\xC0\x00\x00\x01", false)]
        [TestCase("\xE0\x00\x00\x05", false)]
        [TestCase("\xF0\x00\x7F\x7F", false)]
        [TestCase("\xC7\x68\x00\x00", false)]
        [TestCase("\xC7\x68\x00\x01", false)]
        [TestCase("\xE7\x68\x00\x05", false)]
        [TestCase("\xF7\x68\x7F\x7F", false)]
        [TestCase("\xC7\x7F\x00\x00", false)]
        [TestCase("\xC7\x7F\x00\x01", false)]
        [TestCase("\xE7\x7F\x00\x05", false)]
        [TestCase("\xF7\x7F\x7F\x7F", false)]
        public void IsDigital(string data, bool expected)
		{
			byte[] bytes = StringUtils.ToBytes(data);
			Assert.AreEqual(expected, DigitalXSig.IsDigital(bytes));
	    }
	}
}
