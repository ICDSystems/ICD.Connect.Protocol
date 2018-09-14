using System.Text;
using ICD.Common.Properties;
using ICD.Connect.Protocol.Network.WebPorts;
using Newtonsoft.Json;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Network.Tests.WebPorts.Http
{
	[TestFixture]
    public sealed class CoreHttpPortTest
    {
		// ReSharper disable once ClassNeverInstantiated.Local
		private sealed class Post
		{
			[UsedImplicitly]
			public int UserId { get; set; }

			[UsedImplicitly]
			public int Id { get; set; }

			[UsedImplicitly]
			public string Title { get; set; }

			[UsedImplicitly]
			public string Body { get; set; }
		}

		[TestCase("http://127.0.0.1/")]
		[TestCase("http://google.com/")]
		public void AddressTest(string address)
		{
			HttpPort port = new HttpPort
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
			HttpPort port = new HttpPort
			{
				Accept = accept
			};

			Assert.AreEqual(accept, port.Accept);

			port.Dispose();
		}

		[TestCase("test")]
		public void UsernameTest(string username)
		{
			HttpPort port = new HttpPort
			{
				Username = username
			};

			Assert.AreEqual(username, port.Username);

			port.Dispose();
		}

		[TestCase("test")]
		public void PasswordTest(string password)
		{
			HttpPort port = new HttpPort
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
			const string address = "https://test.icdpf.net/";
			const string request = "/posts/1";

			HttpPort port = new HttpPort
			{
				Address = address,
				Accept = "application/json"
			};

			string result;
			Assert.IsTrue(port.Get(request, out result, null));

			Post post = JsonConvert.DeserializeObject<Post>(result);

			Assert.AreEqual(1, post.Id);
			Assert.AreEqual(1, post.UserId);
			Assert.AreEqual("sunt aut facere repellat provident occaecati excepturi optio reprehenderit", post.Title);
			Assert.AreEqual("quia et suscipit\nsuscipit recusandae consequuntur expedita et cum\nreprehenderit molestiae ut ut quas totam\nnostrum rerum est autem sunt rem eveniet architecto", post.Body);

			port.Dispose();
		}

		[Test]
		public void PostTest()
		{
			const string address = "https://test.icdpf.net/";
			const string request = "/posts";

			HttpPort port = new HttpPort
			{
				Address = address,
				Accept = "application/json"
			};

			const string dataString = @"{title: 'foo', body: 'bar', userId: 1}";
			byte[] data = Encoding.ASCII.GetBytes(dataString);

			string result;
			Assert.IsTrue(port.Post(request, data, out result));

			Post post = JsonConvert.DeserializeObject<Post>(result);

			Assert.AreEqual(101, post.Id);

			port.Dispose();
		}

		[Test]
		public void DispatchSoapTest()
		{
			Assert.Inconclusive();
		}
	}
}
