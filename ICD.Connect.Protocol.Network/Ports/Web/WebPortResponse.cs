using System.Collections.Generic;
using System.Text;

namespace ICD.Connect.Protocol.Network.Ports.Web
{
	public sealed class WebPortResponse
	{
		public static WebPortResponse Failed { get { return new WebPortResponse(); } }

		public bool Success { get; set; }
		public int StatusCode { get; set; }
		public byte[] Data { get; set; }
		public IDictionary<string, string[]> Headers { get; set; }
		public string ResponseUrl { get; set; }

		/// <summary>
		/// Converts the byte array to an extended-ASCII string for backwards compatibility.
		/// </summary>
		public string DataAsString
		{
			get { return Data == null ? null : Encoding.GetEncoding(28591).GetString(Data, 0, Data.Length); }
		}
	}
}
