#if STANDARD
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;

namespace ICD.Connect.Protocol.Network.WebPorts
{
	public sealed partial class HttpPort
    {
		private readonly HttpClient m_Client;
		private readonly SafeCriticalSection m_ClientBusySection;

		private string m_Username;
		private string m_Password;

		#region Properties

		/// <summary>
		/// The server address.
		/// </summary>
		public string Address { get; set; }

		/// <summary>
		/// Content type for the server to respond with.
		/// </summary>
		public string Accept
		{
			get
			{
				MediaTypeWithQualityHeaderValue accept = m_Client.DefaultRequestHeaders.Accept.FirstOrDefault();
				return accept == null ? null : accept.MediaType;
			}
			set
			{
				m_Client.DefaultRequestHeaders.Accept.Clear();
				if (value != null)
					m_Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(value));
			}
		}

		/// <summary>
		/// Username for the server.
		/// </summary>
		public string Username
		{
			get { return m_Username; }
			set
			{
				if (value == m_Username)
					return;

				m_Username = value;

				SetAuth(m_Username, m_Password);
			}
		}

		/// <summary>
		/// Password for the server.
		/// </summary>
		public string Password
		{
			get { return m_Password; }
			set
			{
				if (value == m_Password)
					return;

				m_Password = value;

				SetAuth(m_Username, m_Password);
			}
		}

		/// <summary>
		/// Returns true if currently waiting for a response from the server.
		/// </summary>
		public bool Busy
		{
			get
			{
				if (!m_ClientBusySection.TryEnter())
					return true;

				m_ClientBusySection.Leave();
				return false;
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public HttpPort()
		{
			m_LastRequestSucceeded = true;
			
			m_Client = new HttpClient
			{
				Timeout = TimeSpan.FromSeconds(2)
			};

			m_ClientBusySection = new SafeCriticalSection();

			UpdateCachedOnlineStatus();
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			m_Client.CancelPendingRequests();
			m_Client.Dispose();
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
				Uri uri = new Uri(new Uri(GetAddressWithProtocol()), localUrl);
				return Request(localUrl, s => m_Client.GetStringAsync(uri).Result);
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
				Uri uri = new Uri(new Uri(GetAddressWithProtocol()), localUrl);
				HttpContent content = new ByteArrayContent(data);
				return Request(localUrl, s => m_Client.PostAsync(uri, content).Result.Content.ReadAsStringAsync().Result);
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
				Accept = SOAP_ACCEPT;

				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, GetAddressWithProtocol())
				{
					Content = new StringContent(content, Encoding.ASCII, SOAP_CONTENT_TYPE)
				};

				request.Headers.Add(SOAP_ACTION_HEADER, action);

				return Request(content, s => Dispatch(request));
			}
			finally
			{
				m_ClientBusySection.Leave();
			}
		}

		/// <summary>
		/// Dispatches the request and returns the result.
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		private string Dispatch(HttpRequestMessage request)
		{
			m_ClientBusySection.Enter();

			try
			{
				HttpResponseMessage response = m_Client.SendAsync(request).Result;

				if (response == null)
				{
					Logger.AddEntry(eSeverity.Error, "{0} {1} received null response. Is the port busy?", this, request.RequestUri);
					SetLastRequestSucceeded(false);
					return null;
				}

				if ((int)response.StatusCode < 300)
				{
					SetLastRequestSucceeded(true);
					return response.Content.ReadAsStringAsync().Result;
				}

				Logger.AddEntry(eSeverity.Error, "{0} {1} got response with error code {2}", this, request.RequestUri, response.StatusCode);
				SetLastRequestSucceeded(false);
				return null;
			}
			finally
			{
				m_ClientBusySection.Leave();
			}
		}

		#endregion

		#region Private Methods

		private void SetAuth(string username, string password)
		{
			string auth = string.Format("{0}:{1}", username, password);
			byte[] byteArray = Encoding.ASCII.GetBytes(auth);
			m_Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
		}

		#endregion
	}
}
#endif
