﻿using System;
using System.Collections.Generic;
using ICD.Common.Utils.Services.Logging;
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
		/// The server address.
		/// </summary>
		public string Address { get; set; }

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
		/// Username for the server.
		/// </summary>
		public string Username
		{
			get { return m_HttpClient.UserName; }
			set
			{
				m_HttpClient.UserName = value;
				m_HttpsClient.UserName = value;
			}
		}

		/// <summary>
		/// Password for the server.
		/// </summary>
		public string Password
		{
			get { return m_HttpClient.Password; }
			set
			{
				m_HttpClient.Password = value;
				m_HttpsClient.Password = value;
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
			return Get(localUrl, new Dictionary<string, List<string>>(), out response);
		}

		/// <summary>
		/// Sends a GET request to the server.
		/// </summary>
		/// <param name="localUrl"></param>
		/// <param name="headers"></param>
		/// <param name="response"></param>
		public bool Get(string localUrl, IDictionary<string, List<string>> headers, out string response)
		{
			m_ClientBusySection.Enter();

			try
			{
				string url = GetRequestUrl(localUrl);
				PrintTx(url);

				if (IsHttpsUrl(url))
				{
					HttpsClientRequest request = new HttpsClientRequest
					{
						KeepAlive = m_HttpsClient.KeepAlive
					};

					request.Url.Parse(url);
					request.Header.SetHeaderValue("Accept", Accept);
					request.Header.SetHeaderValue("User-Agent", m_HttpsClient.UserAgent);

					foreach (var header in headers)
					{
						foreach (var value in header.Value)
						{
							request.Header.SetHeaderValue(header.Key, value);
						}	
					}

					return Dispatch(request, out response);
				}
				else
				{
					HttpClientRequest request = new HttpClientRequest
					{
						KeepAlive = m_HttpClient.KeepAlive
					};

					request.Url.Parse(url);
					request.Header.SetHeaderValue("Accept", Accept);
					
					foreach (var header in headers)
					{
						foreach (var value in header.Value)
						{
							request.Header.SetHeaderValue(header.Key, value);
						}
					}

					return Dispatch(request, out response);
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
		/// <param name="localUrl"></param>
		/// <param name="data"></param>
		/// <param name="response"></param>
		/// <returns></returns>
		public bool Post(string localUrl, byte[] data, out string response)
		{
			m_ClientBusySection.Enter();

			try
			{
				string url = GetRequestUrl(localUrl);
				PrintTx(url);

				if (IsHttpsUrl(url))
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

					return Dispatch(request, out response);
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

					return Dispatch(request, out response);
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
		/// <param name="response"></param>
		/// <returns></returns>
		public bool DispatchSoap(string action, string content, out string response)
		{
			PrintTx(action);

			Accept = SOAP_ACCEPT;
			m_HttpsClient.IncludeHeaders = false;

			m_ClientBusySection.Enter();

			try
			{
				if (IsHttpsUrl(Address))
				{
					HttpsClientRequest request = new HttpsClientRequest
					{
						RequestType = Crestron.SimplSharp.Net.Https.RequestType.Post,
						Url = new UrlParser(Address),
						Header = {ContentType = SOAP_CONTENT_TYPE},
						ContentString = content
					};
					request.Header.SetHeaderValue(SOAP_ACTION_HEADER, action);

					return Dispatch(request, out response);
				}
				else
				{
					HttpClientRequest request = new HttpClientRequest
					{
						RequestType = Crestron.SimplSharp.Net.Http.RequestType.Post,
						Url = new UrlParser(Address),
						Header = {ContentType = SOAP_CONTENT_TYPE},
						ContentString = content
					};
					request.Header.SetHeaderValue(SOAP_ACTION_HEADER, action);

					return Dispatch(request, out response);
				}
			}
			finally
			{
				m_ClientBusySection.Leave();
			}
		}

		#endregion

		/// <summary>
		/// Returns true if the given address is a https url.
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		private static bool IsHttpsUrl(string url)
		{
			if (url == null)
				throw new ArgumentNullException("url");

			return new IcdUriBuilder(url).Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Builds a request url from the given path.
		/// e.g.
		///		"Test/Path"
		/// May result in
		///		https://10.3.14.15/Test/Path
		/// </summary>
		/// <param name="localAddress"></param>
		/// <returns></returns>
		private string GetRequestUrl(string localAddress)
		{
			return new IcdUriBuilder(Address) {Path = localAddress}.ToString();
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
			bool success = false;

			m_ClientBusySection.Enter();

			try
			{
				HttpsClientResponse response = m_HttpsClient.Dispatch(request);

				if (response == null)
				{
					Log(eSeverity.Error, "{0} received null response. Is the port busy?", request.Url);
				}
				else
				{
					result = response.ContentString;

					if (response.Code < 300)
						success = true;
					else
						Log(eSeverity.Error, "{0} got response with error code {1}", request.Url, response.Code);
				}
			}
			catch (Exception e)
			{
				Log(eSeverity.Error, "{0} threw {0} - {1}", request.Url, e.GetType().Name, e.Message);
			}
			finally
			{
				m_ClientBusySection.Leave();
			}

			SetLastRequestSucceeded(success);
			PrintRx(result);

			return success;
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
			bool success = false;

			m_ClientBusySection.Enter();

			try
			{
				HttpClientResponse response = m_HttpClient.Dispatch(request);

				if (response == null)
				{
					Log(eSeverity.Error, "{0} received null response. Is the port busy?", request.Url);
				}
				else
				{
					result = response.ContentString;

					if (response.Code < 300)
						success = true;
					else
						Log(eSeverity.Error, "{0} got response with error code {1}", request.Url, response.Code);
				}
			}
			catch (Exception e)
			{
				Log(eSeverity.Error, "{0} threw {0} - {1}", request.Url, e.GetType().Name, e.Message);
			}
			finally
			{
				m_ClientBusySection.Leave();
			}

			SetLastRequestSucceeded(success);

			if (!string.IsNullOrEmpty(result))
				PrintRx(result);

			return success;
		}
	}
}

#endif
