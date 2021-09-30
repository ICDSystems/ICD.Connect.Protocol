using System;

namespace ICD.Connect.Protocol.Network.Ports.Web.WebQueues
{
	public sealed class IcdWebRequest
	{
		public enum eWebRequestType
		{
			Get,
			Post,
			Put,
			Patch,
			Soap
		}

		public eWebRequestType RequestType { get; set; }

		public string RelativeOrAbsoluteUri { get; set; }

		public byte[] Data { get; set; }

		public string Action { get; set; }

		public string Content { get; set; }

		public Action<WebPortResponse> Callback { get; set; }
	}
}