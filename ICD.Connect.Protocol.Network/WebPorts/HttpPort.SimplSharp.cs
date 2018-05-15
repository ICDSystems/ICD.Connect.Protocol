using System;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Protocol.Network.Settings;
#if SIMPLSHARP
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharp.Net.Https;
using ICD.Common.Utils;

namespace ICD.Connect.Protocol.Network.WebPorts
{
	public sealed partial class HttpPort
	{
		private readonly HttpClient m_HttpClient;
		private readonly HttpsClient m_HttpsClient;
		private readonly SafeCriticalSection m_ClientBusySection;

		#region Properties

		/// <summary>
		/// Content type for the server to respond with. See HttpClient.Accept.
		/// </summary>
		public string Accept
		{
			get { return m_HttpClient.Accept; }
			set
			{
				m_HttpClient.Accept = value;
				m_HttpsClient.Accept = value;
			}
		}

		/// <summary>
		/// Returns true if currently waiting for a response from the server.
		/// </summary>
		public bool Busy { get { return m_HttpClient.ProcessBusy || m_HttpsClient.ProcessBusy; } }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public HttpPort()
		{
			m_HttpClient = new HttpClient
			{
				KeepAlive = false,
				TimeoutEnabled = true,
				Timeout = 2
			};

			m_HttpsClient = new HttpsClient
			{
				KeepAlive = false,
				TimeoutEnabled = true,
				Timeout = 2,
				HostVerification = false,
				PeerVerification = false
			};

			m_ClientBusySection = new SafeCriticalSection();

			UpdateCachedOnlineStatus();
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
				m_HttpClient.Abort();
			}
			catch
			{
			}

			try
			{
				m_HttpsClient.Abort();
			}
			catch
			{
			}
		}

		/// <summary>
		/// Sends a GET request to the server.
		/// </summary>
		/// <param name="localUrl"></param>
		/// <param name="response"></param>
		public bool Get(string localUrl, out string response)
		{
			bool success;

			m_ClientBusySection.Enter();

			try
			{
				string url = GetRequestUrl(localUrl);
				PrintTx(url);

				if (UseHttps())
				{
					HttpsClientRequest request = new HttpsClientRequest
					{
						KeepAlive = m_HttpsClient.KeepAlive
					};

					request.Url.Parse(url);
					request.Header.SetHeaderValue("Accept", Accept);
					request.Header.SetHeaderValue("User-Agent", m_HttpsClient.UserAgent);

					success = Dispatch(request, out response);
				}
				else
				{
					HttpClientRequest request = new HttpClientRequest
					{
						KeepAlive = m_HttpClient.KeepAlive
					};

					request.Url.Parse(url);
					request.Header.SetHeaderValue("Accept", Accept);

					success = Dispatch(request, out response);
				}
			}
			finally
			{
				m_ClientBusySection.Leave();
			}

			SetLastRequestSucceeded(success);
			PrintRx(response);
			return success;
		}

		/// <summary>
		/// Sends a POST request to the server.
		/// </summary>
		/// <param name="localUrl"></param>
		/// <param name="data"></param>
		/// <param name="response"></param>
		/// <returns></returns>
		public bool Post(string localUrl, byte[] data, out string response)
		{
			bool success;

			m_ClientBusySection.Enter();

			try
			{
				string url = GetRequestUrl(localUrl);
				PrintTx(url);

				if (UseHttps())
				{
					HttpsClientRequest request = new HttpsClientRequest
					{
						ContentSource = Crestron.SimplSharp.Net.Https.ContentSource.ContentNone,
						ContentBytes = data,
						RequestType = Crestron.SimplSharp.Net.Https.RequestType.Post,
						KeepAlive = m_HttpsClient.KeepAlive
					};

					request.Url.Parse(url);
					request.Header.SetHeaderValue("Accept", Accept);
					request.Header.SetHeaderValue("User-Agent", m_HttpsClient.UserAgent);

					success = Dispatch(request, out response);
				}
				else
				{
					HttpClientRequest request = new HttpClientRequest
					{
						KeepAlive = m_HttpClient.KeepAlive,
						ContentBytes = data,
						RequestType = Crestron.SimplSharp.Net.Http.RequestType.Post
					};

					request.Url.Parse(url);
					request.Header.SetHeaderValue("Accept", Accept);
					request.Header.SetHeaderValue("User-Agent", m_HttpClient.UserAgent);

					success = Dispatch(request, out response);
				}
			}
			finally
			{
				m_ClientBusySection.Leave();
			}

			SetLastRequestSucceeded(success);
			PrintRx(response);
			return success;
		}

		/// <summary>
		/// Sends a SOAP request to the server.
		/// </summary>
		/// <param name="action"></param>
		/// <param name="content"></param>
		/// <param name="response"></param>
		/// <returns></returns>
		public bool DispatchSoap(string action, string content, out string response)
		{
			bool success;

			PrintTx(action);

			Accept = SOAP_ACCEPT;
			m_HttpsClient.IncludeHeaders = false;

			m_ClientBusySection.Enter();

			try
			{
				string url = m_UriProperties.GetAddressFromUri();
				UrlParser urlParser = new UrlParser(url);

				if (UseHttps())
				{
					HttpsClientRequest request = new HttpsClientRequest
					{
						RequestType = Crestron.SimplSharp.Net.Https.RequestType.Post,
						Url = urlParser,
						Header = {ContentType = SOAP_CONTENT_TYPE},
						ContentString = content
					};
					request.Header.SetHeaderValue(SOAP_ACTION_HEADER, action);

					success = Dispatch(request, out response);
				}
				else
				{
					HttpClientRequest request = new HttpClientRequest
					{
						RequestType = Crestron.SimplSharp.Net.Http.RequestType.Post,
						Url = urlParser,
						Header = {ContentType = SOAP_CONTENT_TYPE},
						ContentString = content
					};
					request.Header.SetHeaderValue(SOAP_ACTION_HEADER, action);

					success = Dispatch(request, out response);
				}
			}
			finally
			{
				m_ClientBusySection.Leave();
			}

			SetLastRequestSucceeded(success);
			PrintRx(response);
			return success;
		}

		#endregion

		/// <summary>
		/// Returns true if the configured URI scheme is https.
		/// </summary>
		/// <returns></returns>
		private bool UseHttps()
		{
			return string.Equals(m_UriProperties.UriScheme, "https", StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Dispatches the request and returns the result.
		/// </summary>
		/// <param name="request"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		private bool Dispatch(HttpsClientRequest request, out string result)
		{
			result = null;

			m_ClientBusySection.Enter();

			try
			{
				HttpsClientResponse response = m_HttpsClient.Dispatch(request);

				if (response == null)
				{
					Log(eSeverity.Error, "{0} received null response. Is the port busy?", request.Url);
					return false;
				}

				result = response.ContentString;

				if (response.Code < 300)
					return true;

				Log(eSeverity.Error, "{0} got response with error code {1}", request.Url, response.Code);
				return false;
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
		/// <param name="result"></param>
		/// <returns></returns>
		private bool Dispatch(HttpClientRequest request, out string result)
		{
			result = null;

			m_ClientBusySection.Enter();

			try
			{
				HttpClientResponse response = m_HttpClient.Dispatch(request);

				if (response == null)
				{
					Log(eSeverity.Error, "{0} received null response. Is the port busy?", request.Url);
					return false;
				}

				if (response.Code < 300)
				{
					result = response.ContentString;
					return true;
				}

				Log(eSeverity.Error, "{0} got response with error code {1}", request.Url, response.Code);
				return false;
			}
			finally
			{
				m_ClientBusySection.Leave();
			}
		}
	}
}

#endif
