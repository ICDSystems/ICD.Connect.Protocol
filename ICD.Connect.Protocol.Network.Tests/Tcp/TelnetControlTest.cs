using ICD.Connect.Protocol.Network.Tcp;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Network.Tests.Tcp
{
	[TestFixture]
	public sealed class TelnetControlTest
	{
		[TestCase(TelnetControl.eCommand.Will, (byte)0xFE, (byte)0xFF, (byte)0xFB, (byte)0xFE)]
		public void BuildNegotiation(TelnetControl.eCommand command, byte option, params byte[] expected)
		{
			Assert.AreEqual(expected, TelnetControl.BuildNegotiation(command, option));
		}

		[Test]
		public void RejectStringTest()
		{
			Assert.Inconclusive();
		}

		[Test]
		public void RejectBytesTest()
		{
			Assert.Inconclusive();
		}

		[TestCase((byte)0xFE, (byte)0xFE)]
		[TestCase((byte)0xFC, (byte)0xFC)]
		[TestCase((byte)0xFB, (byte)0xFE)]
		[TestCase((byte)0xFD, (byte)0xFC)]
		public void RejectByteTest(byte command, byte expected)
		{
			Assert.AreEqual(expected, TelnetControl.Reject(command));
		}

		[TestCase(TelnetControl.eCommand.Dont, TelnetControl.eCommand.Dont)]
		[TestCase(TelnetControl.eCommand.Wont, TelnetControl.eCommand.Wont)]
		[TestCase(TelnetControl.eCommand.Will, TelnetControl.eCommand.Dont)]
		[TestCase(TelnetControl.eCommand.Do, TelnetControl.eCommand.Wont)]
		public void RejectEnumTest(TelnetControl.eCommand command, TelnetControl.eCommand expected)
		{
			Assert.AreEqual(expected, TelnetControl.Reject(command));
		}
	}
}
