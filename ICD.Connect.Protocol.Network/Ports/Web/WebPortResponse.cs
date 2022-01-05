using System.Collections.Generic;
using System.Text;

namespace ICD.Connect.Protocol.Network.Ports.Web
{
	public sealed class WebPortResponse
	{
		private string m_DataAsString;

		public static WebPortResponse Failed { get { return new WebPortResponse(); } }

		public bool GotResponse { get; set; }
		public int StatusCode { get; set; }
		public byte[] Data { get; set; }
		public IDictionary<string, string[]> Headers { get; set; }
		public string ResponseUrl { get; set; }

		public bool IsSuccessCode { get { return StatusCode < 300; } }

		/// <summary>
		/// Converts the byte array to an extended-ASCII string for backwards compatibility.
		/// </summary>
		public string DataAsString
		{
			get
			{
				if (m_DataAsString == null)
				{
					m_DataAsString =
						Data == null ? null : Encoding.GetEncoding(28591).GetString(Data, 0, Data.Length);
				}
				return m_DataAsString;
			}
		}
	}
}
