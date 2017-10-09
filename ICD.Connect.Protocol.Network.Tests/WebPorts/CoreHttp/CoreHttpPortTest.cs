using System.Text;
using ICD.Common.Properties;
using ICD.Connect.Protocol.Network.WebPorts.CoreHttp;
using Newtonsoft.Json;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Network.Tests.WebPorts.CoreHttp
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
			const string address = "http://jsonplaceholder.typicode.com/";
			const string request = "/posts/1";

			CoreHttpPort port = new CoreHttpPort
			{
				Address = address,
				Accept = "application/json"
			};

			string result = port.Get(request);
			Post post = JsonConvert.DeserializeObject<Post>(result);

			Assert.AreEqual(1, post.Id);
			Assert.AreEqual(1, post.UserId);
			Assert.AreEqual("sunt aut facere repellat provident occaecati excepturi optio reprehenderit", post.Title);
			Assert.AreEqual("quia et suscipit\nsuscipit recusandae consequuntur expedita et cum\nreprehenderit molestiae ut ut quas totam\nnostrum rerum est autem sunt rem eveniet architecto", post.Body);
		}

		[Test]
		public void PostTest()
		{
			const string address = "http://jsonplaceholder.typicode.com/";
			const string request = "/posts";

			CoreHttpPort port = new CoreHttpPort
			{
				Address = address,
				Accept = "application/json"
			};

			const string dataString = @"{title: 'foo', body: 'bar', userId: 1}";
			byte[] data = Encoding.ASCII.GetBytes(dataString);

			string result = port.Post(request, data);
			Post post = JsonConvert.DeserializeObject<Post>(result);

			Assert.AreEqual(101, post.Id);
		}

		[Test]
		public void DispatchSoapTest()
		{
			Assert.Inconclusive();
		}
	}
}
