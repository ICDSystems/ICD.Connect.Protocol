using System;
using System.Text;
using ICD.Common.Properties;
using ICD.Connect.Protocol.Network.Ports.Web;
using Newtonsoft.Json;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Network.Tests.Ports.Web
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
				Accept = "application/json",
				Uri = new Uri(address)
			};

			WebPortResponse response = port.Get(request);
			Assert.IsTrue(response.Success);

			Post post = JsonConvert.DeserializeObject<Post>(response.DataAsString);

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
				Accept = "application/json",
				Uri = new Uri(address)
			};

			const string dataString = @"{title: 'foo', body: 'bar', userId: 1}";
			byte[] data = Encoding.ASCII.GetBytes(dataString);

			var response = port.Post(request, data);
			Assert.IsTrue(response.Success);

			Post post = JsonConvert.DeserializeObject<Post>(response.DataAsString);

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
