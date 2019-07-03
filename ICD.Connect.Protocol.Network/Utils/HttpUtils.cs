using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ICD.Connect.Protocol.Network.Utils
{
	public static class HttpUtils
	{
		/// <summary>
		/// Given a mapping of keys to values, generates a & delimited string.
		/// 
		/// E.g.
		/// client_id=bfd05f66-d691-4af5-8d27-6cd99094999e&grant_type=client_credentials&client_secret=ReLEyn7Z%2FKbYEYStQD%3FZTJlHk%2BLH8%3D95&scope=https%3A%2F%2Fgraph.microsoft.com%2F.default
		/// </summary>
		/// <param name="body"></param>
		/// <returns></returns>
		public static byte[] GetFormUrlEncodedContentBytes(IEnumerable<KeyValuePair<string, string>> body)
		{
			string contentString = GetFormUrlEncodedContent(body);
			return Encoding.UTF8.GetBytes(contentString);
		}

		/// <summary>
		/// Given a mapping of keys to values, generates a & delimited string.
		/// 
		/// E.g.
		/// client_id=bfd05f66-d691-4af5-8d27-6cd99094999e&grant_type=client_credentials&client_secret=ReLEyn7Z%2FKbYEYStQD%3FZTJlHk%2BLH8%3D95&scope=https%3A%2F%2Fgraph.microsoft.com%2F.default
		/// </summary>
		/// <param name="body"></param>
		/// <returns></returns>
		public static string GetFormUrlEncodedContent(IEnumerable<KeyValuePair<string, string>> body)
		{
			string[] items = body.Select(kvp => GetFormUrlEncodedContentItem(kvp.Key, kvp.Value)).ToArray();
			return string.Join("&", items);
		}

		/// <summary>
		/// Given a key and a value, generates an escaped string.
		/// 
		/// E.g.
		/// client_id=bfd05f66-d691-4af5-8d27-6cd99094999e
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		private static string GetFormUrlEncodedContentItem(string key, string value)
		{
			return string.Format("{0}={1}", key, Uri.EscapeDataString(value));
		}
	}
}