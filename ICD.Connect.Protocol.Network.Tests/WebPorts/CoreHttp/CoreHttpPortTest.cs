using ICD.Connect.Protocol.Network.WebPorts.CoreHttp;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Network.Tests.WebPorts.CoreHttp
{
	[TestFixture]
    public sealed class CoreHttpPortTest
    {
		[TestCase("http://127.0.0.1/")]
		[TestCase("http://google.com/")]
		public void AddressTest(string address)
		{
			CoreHttpPort port = new CoreHttpPort
			{
				Address = address
			};

			Assert.AreEqual(address, port.Address);

			port.Dispose();
		}

		[TestCase("application/json")]
		[TestCase("text/plain")]
		public void AcceptTest(string accept)
		{
			CoreHttpPort port = new CoreHttpPort
			{
				Accept = accept
			};

			Assert.AreEqual(accept, port.Accept);

			port.Dispose();
		}

		[TestCase("test")]
		public void UsernameTest(string username)
		{
			CoreHttpPort port = new CoreHttpPort
			{
				Username = username
			};

			Assert.AreEqual(username, port.Username);

			port.Dispose();
		}

		[TestCase("test")]
		public void PasswordTest(string password)
		{
			CoreHttpPort port = new CoreHttpPort
			{
				Password = password
			};

			Assert.AreEqual(password, port.Password);

			port.Dispose();
		}

		[Test]
		public void BusyTest()
		{
			Assert.Inconclusive();
		}

		[Test]
		public void GetTest()
		{
			Assert.Inconclusive();
		}

		[Test]
		public void PostTest()
		{
			Assert.Inconclusive();
		}

		[Test]
		public void DispatchSoapTest()
		{
			Assert.Inconclusive();
		}
	}
}
