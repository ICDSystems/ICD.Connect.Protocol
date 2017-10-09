#if SIMPLSHARP
using Crestron.SimplSharp.Net.Http;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;

namespace ICD.Connect.Protocol.Network.WebPorts
{
	public sealed partial class HttpPort
	{
		private readonly HttpClient m_Client;
		private readonly SafeCriticalSection m_ClientBusySection;

		#region Properties

		/// <summary>
		/// The server address.
		/// </summary>
		public string Address { get; set; }

		/// <summary>
		/// Content type for the server to respond with. See HttpClient.Accept.
		/// </summary>
		public string Accept { get { return m_Client.Accept; } set { m_Client.Accept = value; } }

		/// <summary>
		/// Username for the server.
		/// </summary>
		public string Username { get { return m_Client.UserName; } set { m_Client.UserName = value; } }

		/// <summary>
		/// Password for the server.
		/// </summary>
		public string Password { get { return m_Client.Password; } set { m_Client.Password = value; } }

		/// <summary>
		/// Returns true if currently waiting for a response from the server.
		/// </summary>
		public bool Busy { get { return m_Client.ProcessBusy; } }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public HttpPort()
		{
			m_Client = new HttpClient
			{
				KeepAlive = false,
				TimeoutEnabled = true,
				Timeout = 2
			};

			m_ClientBusySection = new SafeCriticalSection();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			try
			{
				m_Client.Abort();
			}
				// ReSharper disable EmptyGeneralCatchClause
			catch
				// ReSharper restore EmptyGeneralCatchClause
			{
			}
		}

		/// <summary>
		/// Sends a GET request to the server.
		/// </summary>
		/// <param name="localUrl"></param>
		public string Get(string localUrl)
		{
			m_ClientBusySection.Enter();

			try
			{
				string url = GetRequestUrl(localUrl);
				return Request(url, s => m_Client.Get(s));
			}
			finally
			{
				m_ClientBusySection.Leave();
			}
		}

		/// <summary>
		/// Sends a POST request to the server.
		/// </summary>
		/// <param name="localUrl"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		public string Post(string localUrl, byte[] data)
		{
			m_ClientBusySection.Enter();

			try
			{
				string url = GetRequestUrl(localUrl);
				return Request(url, s => m_Client.Post(s, data));
			}
			finally
			{
				m_ClientBusySection.Leave();
			}
		}

		/// <summary>
		/// Sends a SOAP request to the server.
		/// </summary>
		/// <param name="action"></param>
		/// <param name="content"></param>
		/// <returns></returns>
		public string DispatchSoap(string action, string content)
		{
			m_ClientBusySection.Enter();

			try
			{
				m_Client.Accept = SOAP_ACCEPT;

				HttpClientRequest request = new HttpClientRequest
				{
					RequestType = RequestType.Post,
					Url = new UrlParser(Address),
					Header = {ContentType = SOAP_CONTENT_TYPE},
					ContentString = content
				};
				request.Header.SetHeaderValue(SOAP_ACTION_HEADER, action);

				return Request(content, s => Dispatch(request));
			}
			finally
			{
				m_ClientBusySection.Leave();
			}
		}

		#endregion

		/// <summary>
		/// Builds a request url from the given path.
		/// e.g.
		///		"Test/Path"
		/// May result in
		///		https://10.3.14.15/Test/Path
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		private string GetRequestUrl(string data)
		{
			string output = Address;

			if (string.IsNullOrEmpty(data))
				return output;

			if (data.StartsWith("/"))
				data = data.Substring(1);

			return string.Format("{0}/{1}", output, data);
		}

		/// <summary>
		/// Dispatches the request and returns the result.
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		private string Dispatch(HttpClientRequest request)
		{
			m_ClientBusySection.Enter();

			try
			{
				HttpClientResponse response = m_Client.Dispatch(request);

				if (response == null)
				{
					Logger.AddEntry(eSeverity.Error, "{0} {1} received null response. Is the port busy?", this, request.Url);
					return null;
				}

				if (response.Code < 300)
					return response.ContentString;

				Logger.AddEntry(eSeverity.Error, "{0} {1} got response with error code {2}", this, request.Url, response.Code);
				return null;
			}
			finally
			{
				m_ClientBusySection.Leave();
			}
		}
	}
}

#endif
