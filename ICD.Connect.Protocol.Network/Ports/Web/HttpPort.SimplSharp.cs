#if SIMPLSHARP
using System.Linq;
using ICD.Connect.API.Commands;
using System;
using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using Crestron.SimplSharp.Net;
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharp.Net.Https;
using ICD.Common.Utils;
using HttpsContentSource = Crestron.SimplSharp.Net.Https.ContentSource;
using HttpsRequestType = Crestron.SimplSharp.Net.Https.RequestType;
using HttpContentSource = Crestron.SimplSharp.Net.Http.ContentSource;
using HttpRequestType = Crestron.SimplSharp.Net.Http.RequestType;

namespace ICD.Connect.Protocol.Network.Ports.Web
{
	public sealed partial class HttpPort
	{
		private const string SOAP_CONTENT_TYPE = "text/xml; charset=utf-8";
	    private const string DEFAULT_USER_AGENT = "Crestron SimplSharp Client";
        private const string DEFAULT_ACCEPT = "*/*";
	    private const int DEFAULT_TIMEOUT = 60;

		//private readonly HttpsClient m_HttpsClient;
		//private readonly HttpClient m_HttpClient;
		private readonly SafeCriticalSection m_ClientBusySection;

	    private string m_Accept;

		#region Properties

		/// <summary>
		/// Content type for the server to respond with. See HttpClient.Accept.
		/// </summary>
		public override string Accept
		{
			get { return m_Accept; }
			set
			{
				m_Accept = string.IsNullOrEmpty(value) ? DEFAULT_ACCEPT : value;
			}
		}

		/// <summary>
		/// Returns true if currently waiting for a response from the server.
		/// </summary>
		public override bool Busy {
		    get
		    {
                //todo: Implement Something here, probably
		        return false;
		        //return m_HttpsClient.ProcessBusy || m_HttpClient.ProcessBusy;
		    } }

        /// <summary>
        /// If requests should use the keepalive
        /// </summary>
        public bool KeepAlive { get; set; }

        public string UserAgent { get; set; }

        public bool VerboseMode { get; set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public HttpPort()
		{
		    m_Accept = DEFAULT_ACCEPT;
		    UserAgent = DEFAULT_USER_AGENT;

			m_ClientBusySection = new SafeCriticalSection();

			UpdateCachedOnlineStatus();
		}

		#endregion

	    private HttpClient InstantiateHttpClient()
	    {
	        return InstantiateHttpClient(Accept);
	    }

	    private HttpClient InstantiateHttpClient(string accept)
	    {
	        return InstantiateHttpClient(accept, KeepAlive, VerboseMode);
	    }

	    private static HttpClient InstantiateHttpClient(string accept, bool keepAlive, bool verboseMode)
	    {
            return new HttpClient
            {
                KeepAlive = keepAlive,
                Accept = accept,
                TimeoutEnabled = true,
                Timeout = DEFAULT_TIMEOUT,
                UserAgent = DEFAULT_USER_AGENT,
                Verbose = verboseMode
            };
	    }

        private HttpsClient InstantiateHttpsClient()
        {
            return InstantiateHttpsClient(Accept);
        }

        private HttpsClient InstantiateHttpsClient(string accept)
        {
            return InstantiateHttpsClient(accept, KeepAlive, VerboseMode);
        }

	    private static HttpsClient InstantiateHttpsClient(string accept, bool keepAlive, bool verboseMode)
	    {
            return new HttpsClient
            {
                KeepAlive = keepAlive,
                Accept = accept,
                TimeoutEnabled = true,
                Timeout = DEFAULT_TIMEOUT,
                HostVerification = false,
                PeerVerification = false,
                UserAgent = DEFAULT_USER_AGENT,
                Verbose =  verboseMode
            };
        }

		#region Methods

		/// <summary>
		/// Sends a GET request to the server.
		/// </summary>
		/// <param name="relativeOrAbsoluteUri"></param>
		/// <param name="headers"></param>
		/// <param name="data"></param>
		public override WebPortResponse Get(string relativeOrAbsoluteUri, IDictionary<string, List<string>> headers, byte[] data)
		{
			m_ClientBusySection.Enter();

			try
			{
				Uri url = GetRequestUrl(relativeOrAbsoluteUri);
				PrintTx(() => url);

				if (url.Scheme == Uri.UriSchemeHttp)
				{
					HttpClientRequest request = new HttpClientRequest
					{
						KeepAlive = KeepAlive,
						ContentBytes = data,
						ContentSource = data == null ? HttpContentSource.ContentNone : HttpContentSource.ContentBytes,
						RequestType = HttpRequestType.Get,
						Url = {Url = url.ToString()}
					};

					request.Header.SetHeaderValue("Accept", Accept);
					request.Header.SetHeaderValue("Expect", "");

					foreach (KeyValuePair<string, List<string>> header in headers)
						request.Header.SetHeaderValue(header.Key, string.Join(";", header.Value.ToArray()));

					return Dispatch(request);
				}
				else
				{
					HttpsClientRequest request = new HttpsClientRequest
					{
						KeepAlive = KeepAlive,
						ContentBytes = data,
						ContentSource = data == null ? HttpsContentSource.ContentNone : HttpsContentSource.ContentBytes,
						RequestType = HttpsRequestType.Get,
						Url = {Url = url.ToString()}
					};

					request.Header.SetHeaderValue("Accept", Accept);
					request.Header.SetHeaderValue("Expect", "");

					foreach (KeyValuePair<string, List<string>> header in headers)
						request.Header.SetHeaderValue(header.Key, string.Join(";", header.Value.ToArray()));

					return Dispatch(request);
				}
			}
			finally
			{
				m_ClientBusySection.Leave();
			}
		}

		/// <summary>
		/// Sends a POST request to the server.
		/// </summary>
		/// <param name="relativeOrAbsoluteUri"></param>
		/// <param name="headers"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		public override WebPortResponse Post(string relativeOrAbsoluteUri, Dictionary<string, List<string>> headers, byte[] data)
		{
			m_ClientBusySection.Enter();

			try
			{
				Uri url = GetRequestUrl(relativeOrAbsoluteUri);
				PrintTx(() => url);

				if (url.Scheme == Uri.UriSchemeHttp)
				{
					HttpClientRequest request = new HttpClientRequest
					{
						KeepAlive = KeepAlive,
						ContentBytes = data,
						ContentSource = data == null ? HttpContentSource.ContentNone : HttpContentSource.ContentBytes,
						RequestType = HttpRequestType.Post,
						Url = {Url = url.ToString()}
					};

					request.Header.SetHeaderValue("Accept", Accept);
					request.Header.SetHeaderValue("Expect", "");
					request.Header.SetHeaderValue("User-Agent", UserAgent);

					foreach (KeyValuePair<string, List<string>> header in headers)
						request.Header.SetHeaderValue(header.Key, string.Join(";", header.Value.ToArray()));

					return Dispatch(request);
				}
				else
				{
					HttpsClientRequest request = new HttpsClientRequest
					{
						KeepAlive = KeepAlive,
						ContentBytes = data,
						ContentSource = data == null ? HttpsContentSource.ContentNone : HttpsContentSource.ContentBytes,
						RequestType = HttpsRequestType.Post,
						Url = {Url = url.ToString()}
					};

					request.Header.SetHeaderValue("Accept", Accept);
					request.Header.SetHeaderValue("Expect", "");
					request.Header.SetHeaderValue("User-Agent", UserAgent);

					foreach (KeyValuePair<string, List<string>> header in headers)
						request.Header.SetHeaderValue(header.Key, string.Join(";", header.Value.ToArray()));

					return Dispatch(request);
				}
			}
			finally
			{
				m_ClientBusySection.Leave();
			}
		}

		/// <summary>
		/// Sends a PATCH request to the server.
		/// </summary>
		/// <param name="relativeOrAbsoluteUri"></param>
		/// <param name="headers"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		public override WebPortResponse Patch(string relativeOrAbsoluteUri, Dictionary<string, List<string>> headers, byte[] data)
		{
			m_ClientBusySection.Enter();

			try
			{
				Uri url = GetRequestUrl(relativeOrAbsoluteUri);
				PrintTx(() => url);

				if (url.Scheme == Uri.UriSchemeHttp)
				{
					HttpClientRequest request = new HttpClientRequest
					{
						KeepAlive = KeepAlive,
						ContentBytes = data,
						ContentSource = data == null ? HttpContentSource.ContentNone : HttpContentSource.ContentBytes,
						RequestType = HttpRequestType.Patch,
						Url = { Url = url.ToString() }
					};

					request.Header.SetHeaderValue("Accept", Accept);
					request.Header.SetHeaderValue("Expect", "");
					request.Header.SetHeaderValue("User-Agent", UserAgent);

					foreach (KeyValuePair<string, List<string>> header in headers)
						request.Header.SetHeaderValue(header.Key, string.Join(";", header.Value.ToArray()));

					return Dispatch(request);
				}
				else
				{
					HttpsClientRequest request = new HttpsClientRequest
					{
						KeepAlive = KeepAlive,
						ContentBytes = data,
						ContentSource = data == null ? HttpsContentSource.ContentNone : HttpsContentSource.ContentBytes,
						RequestType = HttpsRequestType.Patch,
						Url = { Url = url.ToString() }
					};

					request.Header.SetHeaderValue("Accept", Accept);
					request.Header.SetHeaderValue("Expect", "");
					request.Header.SetHeaderValue("User-Agent", UserAgent);

					foreach (KeyValuePair<string, List<string>> header in headers)
						request.Header.SetHeaderValue(header.Key, string.Join(";", header.Value.ToArray()));

					return Dispatch(request);
				}
			}
			finally
			{
				m_ClientBusySection.Leave();
			}
		}

		/// <summary>
		/// Sends a PUT request to the server.
		/// </summary>
		/// <param name="relativeOrAbsoluteUri"></param>
		/// <param name="headers"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		public override WebPortResponse Put(string relativeOrAbsoluteUri, Dictionary<string, List<string>> headers, byte[] data)
		{
			m_ClientBusySection.Enter();

			try
			{
				Uri url = GetRequestUrl(relativeOrAbsoluteUri);
				PrintTx(() => url);

				if (url.Scheme == Uri.UriSchemeHttp)
				{
					HttpClientRequest request = new HttpClientRequest
					{
						KeepAlive = KeepAlive,
						ContentBytes = data,
						ContentSource = data == null ? HttpContentSource.ContentNone : HttpContentSource.ContentBytes,
						RequestType = HttpRequestType.Put,
						Url = { Url = url.ToString() }
					};

					request.Header.SetHeaderValue("Accept", Accept);
					request.Header.SetHeaderValue("Expect", "");
					request.Header.SetHeaderValue("User-Agent", UserAgent);

					foreach (KeyValuePair<string, List<string>> header in headers)
						request.Header.SetHeaderValue(header.Key, string.Join(";", header.Value.ToArray()));

					return Dispatch(request);
				}
				else
				{
					HttpsClientRequest request = new HttpsClientRequest
					{
						KeepAlive = KeepAlive,
						ContentBytes = data,
						ContentSource = data == null ? HttpsContentSource.ContentNone : HttpsContentSource.ContentBytes,
						RequestType = HttpsRequestType.Put,
						Url = { Url = url.ToString() }
					};

					request.Header.SetHeaderValue("Accept", Accept);
					request.Header.SetHeaderValue("Expect", "");
					request.Header.SetHeaderValue("User-Agent", UserAgent);

					foreach (KeyValuePair<string, List<string>> header in headers)
						request.Header.SetHeaderValue(header.Key, string.Join(";", header.Value.ToArray()));

					return Dispatch(request);
				}
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
		public override WebPortResponse DispatchSoap(string action, string content)
		{
			PrintTx(() => action);

			Accept = SOAP_ACCEPT;

			m_ClientBusySection.Enter();

			try
			{
				string url = Uri == null ? null : Uri.ToString();
				UrlParser urlParser = new UrlParser(url);

				if (urlParser.Protocol == Uri.UriSchemeHttp)
				{
					HttpClientRequest request = new HttpClientRequest
					{
						RequestType = HttpRequestType.Post,
						Url = urlParser,
						Header = {ContentType = SOAP_CONTENT_TYPE},
						ContentString = content
					};
					request.Header.SetHeaderValue(SOAP_ACTION_HEADER, action);
					request.Header.SetHeaderValue("Expect", "");

				    using (var client = InstantiateHttpClient(SOAP_ACCEPT))
				    {
				        return Dispatch(request, client);
				    }
				}
				else
				{
					HttpsClientRequest request = new HttpsClientRequest
					{
						RequestType = HttpsRequestType.Post,
						Url = urlParser,
						Header = { ContentType = SOAP_CONTENT_TYPE },
						ContentString = content
					};
					request.Header.SetHeaderValue(SOAP_ACTION_HEADER, action);
					request.Header.SetHeaderValue("Expect", "");

				    using (var client = InstantiateHttpsClient(SOAP_ACCEPT))
				    {
				        client.IncludeHeaders = false;
                        return Dispatch(request, client);
				    }
				}
			}
			catch (Exception e)
			{
				Logger.Log(eSeverity.Error, "Failed to dispatch SOAP - {0}", e.Message);
			}
			finally
			{
				m_ClientBusySection.Leave();
			}

			SetLastRequestSucceeded(false);
			return WebPortResponse.Failed;
		}

		#endregion

		#region Private Methods

        /// <summary>
        /// Dispatches the request and returns the result.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
	    private WebPortResponse Dispatch(HttpsClientRequest request)
	    {
	        using (var client = InstantiateHttpsClient())
	        {
	            return Dispatch(request, client);
	        }
	    }

	    /// <summary>
	    /// Dispatches the request and returns the result.
	    /// </summary>
	    /// <param name="request"></param>
	    /// <param name="client"></param>
	    /// <returns></returns>
	    private WebPortResponse Dispatch(HttpsClientRequest request, HttpsClient client)
		{
			WebPortResponse output = WebPortResponse.Failed;

			m_ClientBusySection.Enter();

			try
			{
				ConfigureProxySettings(client);

				HttpsClientResponse response = client.Dispatch(request);

				if (response == null)
				{
					Logger.Log(eSeverity.Error, "{0} received null response. Is the port busy?", new Uri(request.Url.Url).ToPrivateString());
				}
				else
				{
					if (response.Code >= 300)
						Logger.Log(eSeverity.Error, "{0} got response with error code {1}", new Uri(request.Url.Url), response.Code);

					output = new WebPortResponse
					{
						GotResponse = true,
						StatusCode = response.Code,
						Data = response.ContentBytes,
						Headers = response.Header.Cast<HttpsHeader>().ToDictionary(h => h.Name, h => new[] {h.Value}),
						ResponseUrl = response.ResponseUrl
					};
				}
			}
			catch (Exception e)
			{
				Logger.Log(eSeverity.Error, "{0} threw {1} - {2}", new Uri(request.Url.Url), e.GetType().Name, e.Message);
			}
			finally
			{
				m_ClientBusySection.Leave();
			}

			SetLastRequestSucceeded(output.GotResponse);
			PrintRx(() => output.DataAsString);

			return output;
		}

	    /// <summary>
	    /// Dispatches the request and returns the result.
	    /// </summary>
	    /// <param name="request"></param>
	    /// <returns></returns>
	    private WebPortResponse Dispatch(HttpClientRequest request)
	    {
	        using (var client = InstantiateHttpClient())
	        {
	            return Dispatch(request, client);
	        }
	    }

	    /// <summary>
	    /// Dispatches the request and returns the result.
	    /// </summary>
	    /// <param name="request"></param>
	    /// <param name="client"></param>
	    /// <returns></returns>
	    private WebPortResponse Dispatch(HttpClientRequest request, HttpClient client)
		{
			WebPortResponse output = WebPortResponse.Failed;

			m_ClientBusySection.Enter();

			try
			{
                // No proxy for HTTP client
				//ConfigureProxySettings();

				HttpClientResponse response = client.Dispatch(request);

				if (response == null)
				{
					Logger.Log(eSeverity.Error, "{0} received null response. Is the port busy?", new Uri(request.Url.Url).ToPrivateString());
				}
				else
				{
					if (response.Code >= 300)
						Logger.Log(eSeverity.Error, "{0} got response with error code {1}", new Uri(request.Url.Url), response.Code);

					output = new WebPortResponse
					{
						GotResponse = true,
						StatusCode = response.Code,
						Data = response.ContentBytes,
						Headers = response.Header.Cast<HttpHeader>().ToDictionary(h => h.Name, h => new[] { h.Value }),
						ResponseUrl = response.ResponseUrl
					};
				}
			}
			catch (Exception e)
			{
				Logger.Log(eSeverity.Error, "{0} threw {1} - {2}", new Uri(request.Url.Url), e.GetType().Name, e.Message);
			}
			finally
			{
				m_ClientBusySection.Leave();
			}

			SetLastRequestSucceeded(output.GotResponse);
			PrintRx(() => output.DataAsString);

			return output;
		}


		private void ConfigureProxySettings(HttpsClient client)
		{
			string urlOrHost =
				ProxyUri == null || ProxyUri.GetIsDefault()
					? null
					: ProxyUri.ToString();

			if (urlOrHost == null)
			{
				client.Proxy = null;
				return;
			}

			ProxySettings settings = client.Proxy ?? (client.Proxy = new ProxySettings(urlOrHost));

			settings.Port = ProxyUri == null ? 0 : ProxyUri.Port;
			settings.AuthenticationMethod = ProxyAuthenticationMethod.ToCrestron();
			settings.UserName = ProxyUri == null ? string.Empty : ProxyUri.GetUserName();
			settings.UserPassword = ProxyUri == null ? string.Empty : ProxyUri.GetPassword();
			settings.HostOrUrl = urlOrHost;
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new ConsoleCommand("EnableVerboseMode", "Enables Verbose mode on the Crestron HttpsClient", () => VerboseMode = true);
			yield return new ConsoleCommand("DisableVerboseMode", "Disables Verbose mode on the Crestron HttpsClient", () => VerboseMode = false);
		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}

#endif
