using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.XSig;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Tests.XSig
{
    [TestFixture]
    public sealed class AnalogXSigTest
    {

        [TestCase((ushort)233, (ushort)11, "\xC0\x09\x01\x69")]
        [TestCase((ushort)0, (ushort)1, "\xC0\x00\x00\x00")]
        [TestCase((ushort)1, (ushort)1, "\xC0\x00\x00\x01")]
        [TestCase((ushort)32773, (ushort)1, "\xE0\x00\x00\x05")]
        [TestCase((ushort)65535, (ushort)1, "\xF0\x00\x7F\x7F")]
        [TestCase((ushort)0, (ushort)1001, "\xC7\x68\x00\x00")]
        [TestCase((ushort)1, (ushort)1001, "\xC7\x68\x00\x01")]
        [TestCase((ushort)32773, (ushort)1001, "\xE7\x68\x00\x05")]
        [TestCase((ushort)65535, (ushort)1001, "\xF7\x68\x7F\x7F")]
        [TestCase((ushort)0, (ushort)1024, "\xC7\x7F\x00\x00")]
        [TestCase((ushort)1, (ushort)1024, "\xC7\x7F\x00\x01")]
        [TestCase((ushort)32773, (ushort)1024, "\xE7\x7F\x00\x05")]
        [TestCase((ushort)65535, (ushort)1024, "\xF7\x7F\x7F\x7F")]
        public void DataTest(ushort value, ushort index, string expected)
        {
            AnalogXSig sig = new AnalogXSig(value, index);
            Assert.AreEqual(StringUtils.ToBytes(expected), sig.Data);
        }

        [TestCase((ushort)233, (ushort)11, "\xC0\x09\x01\x69")]
        [TestCase((ushort)0, (ushort)1, "\xC0\x00\x00\x00")]
        [TestCase((ushort)1, (ushort)1, "\xC0\x00\x00\x01")]
        [TestCase((ushort)32773, (ushort)1, "\xE0\x00\x00\x05")]
        [TestCase((ushort)65535, (ushort)1, "\xF0\x00\x7F\x7F")]
        [TestCase((ushort)0, (ushort)1001, "\xC7\x68\x00\x00")]
        [TestCase((ushort)1, (ushort)1001, "\xC7\x68\x00\x01")]
        [TestCase((ushort)32773, (ushort)1001, "\xE7\x68\x00\x05")]
        [TestCase((ushort)65535, (ushort)1001, "\xF7\x68\x7F\x7F")]
        [TestCase((ushort)0, (ushort)1024, "\xC7\x7F\x00\x00")]
        [TestCase((ushort)1, (ushort)1024, "\xC7\x7F\x00\x01")]
        [TestCase((ushort)32773, (ushort)1024, "\xE7\x7F\x00\x05")]
        [TestCase((ushort)65535, (ushort)1024, "\xF7\x7F\x7F\x7F")]
        public void DataStringTest(ushort value, ushort index, string expected)
        {
            AnalogXSig sig = new AnalogXSig(value, index);
            Assert.AreEqual(expected, sig.DataXSig);
        }

        [TestCase((ushort)233, (ushort)11)]
        [TestCase((ushort)0, (ushort)1)]
        [TestCase((ushort)1, (ushort)1)]
        [TestCase((ushort)32773, (ushort)1)]
        [TestCase((ushort)65535, (ushort)1)]
        [TestCase((ushort)0, (ushort)1001)]
        [TestCase((ushort)1, (ushort)1001)]
        [TestCase((ushort)32773, (ushort)1001)]
        [TestCase((ushort)65535, (ushort)1001)]
        [TestCase((ushort)0, (ushort)1024)]
        [TestCase((ushort)1, (ushort)1024)]
        [TestCase((ushort)32773, (ushort)1024)]
        [TestCase((ushort)65535, (ushort)1024)]
        public void ValueTest(ushort value, ushort index)
        {
            AnalogXSig sig = new AnalogXSig(value, index);
            Assert.AreEqual(value, sig.Value);
        }

        [TestCase((ushort)233, (ushort)11)]
        [TestCase((ushort)0, (ushort)1)]
        [TestCase((ushort)1, (ushort)1)]
        [TestCase((ushort)32773, (ushort)1)]
        [TestCase((ushort)65535, (ushort)1)]
        [TestCase((ushort)0, (ushort)1001)]
        [TestCase((ushort)1, (ushort)1001)]
        [TestCase((ushort)32773, (ushort)1001)]
        [TestCase((ushort)65535, (ushort)1001)]
        [TestCase((ushort)0, (ushort)1024)]
        [TestCase((ushort)1, (ushort)1024)]
        [TestCase((ushort)32773, (ushort)1024)]
        [TestCase((ushort)65535, (ushort)1024)]
        public void IndexTest(ushort value, ushort index)
        {
            AnalogXSig sig = new AnalogXSig(value, index);
            Assert.AreEqual(index, sig.Index);
        }

        [TestCase("\xC0\x09\x01\x69")]
        [TestCase("\xC0\x00\x00\x00")]
        [TestCase("\xC0\x00\x00\x01")]
        [TestCase("\xE0\x00\x00\x05")]
        [TestCase("\xF0\x00\x7F\x7F")]
        [TestCase("\xC7\x68\x00\x00")]
        [TestCase("\xC7\x68\x00\x01")]
        [TestCase("\xE7\x68\x00\x05")]
        [TestCase("\xF7\x68\x7F\x7F")]
        [TestCase("\xC7\x7F\x00\x00")]
        [TestCase("\xC7\x7F\x00\x01")]
        [TestCase("\xE7\x7F\x00\x05")]
        [TestCase("\xF7\x7F\x7F\x7F")]
        public void DataFromDataTest(string data)
        {
            AnalogXSig sig = new AnalogXSig(StringUtils.ToBytes(data));
            Assert.AreEqual(data, StringUtils.ToString(sig.Data));
        }

        [TestCase("\xC0\x09\x01\x69")]
        [TestCase("\xC0\x00\x00\x00")]
        [TestCase("\xC0\x00\x00\x01")]
        [TestCase("\xE0\x00\x00\x05")]
        [TestCase("\xF0\x00\x7F\x7F")]
        [TestCase("\xC7\x68\x00\x00")]
        [TestCase("\xC7\x68\x00\x01")]
        [TestCase("\xE7\x68\x00\x05")]
        [TestCase("\xF7\x68\x7F\x7F")]
        [TestCase("\xC7\x7F\x00\x00")]
        [TestCase("\xC7\x7F\x00\x01")]
        [TestCase("\xE7\x7F\x00\x05")]
        [TestCase("\xF7\x7F\x7F\x7F")]
        public void DataFromDataStringTest(string data)
        {
            AnalogXSig sig = new AnalogXSig(StringUtils.ToBytes(data));
            Assert.AreEqual(data, sig.DataXSig);
        }

        [TestCase((ushort)233, "\xC0\x09\x01\x69")]
        [TestCase((ushort)0, "\xC0\x00\x00\x00")]
        [TestCase((ushort)1, "\xC0\x00\x00\x01")]
        [TestCase((ushort)32773, "\xE0\x00\x00\x05")]
        [TestCase((ushort)65535, "\xF0\x00\x7F\x7F")]
        [TestCase((ushort)0, "\xC7\x68\x00\x00")]
        [TestCase((ushort)1, "\xC7\x68\x00\x01")]
        [TestCase((ushort)32773, "\xE7\x68\x00\x05")]
        [TestCase((ushort)65535, "\xF7\x68\x7F\x7F")]
        [TestCase((ushort)0, "\xC7\x7F\x00\x00")]
        [TestCase((ushort)1, "\xC7\x7F\x00\x01")]
        [TestCase((ushort)32773, "\xE7\x7F\x00\x05")]
        [TestCase((ushort)65535, "\xF7\x7F\x7F\x7F")]
        public void ValueFromDataTest(ushort value, string data)
        {
            AnalogXSig sig = new AnalogXSig(StringUtils.ToBytes(data));
            Assert.AreEqual(value, sig.Value);
        }

        [TestCase((ushort)11, "\xC0\x09\x01\x69")]
        [TestCase((ushort)1, "\xC0\x00\x00\x00")]
        [TestCase((ushort)1, "\xC0\x00\x00\x01")]
        [TestCase((ushort)1, "\xE0\x00\x00\x05")]
        [TestCase((ushort)1, "\xF0\x00\x7F\x7F")]
        [TestCase((ushort)1001, "\xC7\x68\x00\x00")]
        [TestCase((ushort)1001, "\xC7\x68\x00\x01")]
        [TestCase((ushort)1001, "\xE7\x68\x00\x05")]
        [TestCase((ushort)1001, "\xF7\x68\x7F\x7F")]
        [TestCase((ushort)1024, "\xC7\x7F\x00\x00")]
        [TestCase((ushort)1024, "\xC7\x7F\x00\x01")]
        [TestCase((ushort)1024, "\xE7\x7F\x00\x05")]
        [TestCase((ushort)1024, "\xF7\x7F\x7F\x7F")]
        public void IndexFromData(ushort index, string data)
        {
            AnalogXSig sig = new AnalogXSig(StringUtils.ToBytes(data));
            Assert.AreEqual(index, sig.Index);
        }

        [TestCase("\x00", false)]
        [TestCase("\x80\x0A", false)]
        [TestCase("\xA0\x0E", false)]
        [TestCase("\xA0\x00", false)]
        [TestCase("\x80\x00", false)]
        [TestCase("\xAA\x55", false)]
        [TestCase("\x8A\x55", false)]
        [TestCase("\xB5\x2A", false)]
        [TestCase("\x95\x2A", false)]
        [TestCase("\xBF\x7F", false)]
        [TestCase("\x9F\x7F", false)]
        [TestCase("\xC0\x09\x01\x69", true)]
        [TestCase("\xC0\x00\x00\x00", true)]
        [TestCase("\xC0\x00\x00\x01", true)]
        [TestCase("\xE0\x00\x00\x05", true)]
        [TestCase("\xF0\x00\x7F\x7F", true)]
        [TestCase("\xC7\x68\x00\x00", true)]
        [TestCase("\xC7\x68\x00\x01", true)]
        [TestCase("\xE7\x68\x00\x05", true)]
        [TestCase("\xF7\x68\x7F\x7F", true)]
        [TestCase("\xC7\x7F\x00\x00", true)]
        [TestCase("\xC7\x7F\x00\x01", true)]
        [TestCase("\xE7\x7F\x00\x05", true)]
        [TestCase("\xF7\x7F\x7F\x7F", true)]
        public void IsAnalog(string data, bool expected)
        {
            byte[] bytes = StringUtils.ToBytes(data);
            Assert.AreEqual(expected, AnalogXSig.IsAnalog(bytes));
        }
    }
}
