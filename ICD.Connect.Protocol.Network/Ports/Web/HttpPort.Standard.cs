﻿
#if STANDARD
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Protocol.Network.Settings;

namespace ICD.Connect.Protocol.Network.Ports.Web
{
	public sealed partial class HttpPort
	{
		private const string SOAP_CONTENT_TYPE = "text/xml";

		private readonly HttpClient m_Client;
		private readonly HttpClientHandler m_ClientHandler;
		private readonly SafeCriticalSection m_ClientBusySection;

		#region Properties

		/// <summary>
		/// Content type for the server to respond with.
		/// </summary>
		public override string Accept
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
		/// Returns true if currently waiting for a response from the server.
		/// </summary>
		public override bool Busy
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

			m_ClientHandler = new HttpClientHandler
			{
				Proxy = new WebProxy(),
				ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
			};

			m_Client = new HttpClient(m_ClientHandler)
			{
				Timeout = TimeSpan.FromSeconds(5)
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

			m_ClientHandler.Dispose();
		}

		/// <summary>
		/// Sends a GET request to the server.
		/// </summary>
		/// <param name="relativeOrAbsoluteUri"></param>
		/// <param name="headers"></param>
		public override WebPortResponse Get(string relativeOrAbsoluteUri, IDictionary<string, List<string>> headers)
		{
			if (headers == null)
				throw new ArgumentNullException("headers");

			m_ClientBusySection.Enter();

			try
			{
				string url = GetRequestUrl(relativeOrAbsoluteUri);
				PrintTx(url);
				
				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
				
				foreach (KeyValuePair<string, List<string>> header in headers)
					AddHeader(request, header.Key, header.Value);

				return Dispatch(request);
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
				string url = GetRequestUrl(relativeOrAbsoluteUri);
				PrintTx(url);

				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url)
				{
					Content = new ByteArrayContent(data)
				};

				foreach (KeyValuePair<string, List<string>> header in headers)
					AddHeader(request, header.Key, header.Value);

				return Dispatch(request);
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
			PrintTx(action);

			Accept = SOAP_ACCEPT;

			m_ClientBusySection.Enter();

			try
			{
				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, m_UriProperties.GetUri())
				{
					Content = new StringContent(content, Encoding.GetEncoding(28591), SOAP_CONTENT_TYPE)
				};

				AddHeader(request, SOAP_ACTION_HEADER, action);

				return Dispatch(request);
			}
			catch (Exception e)
			{
				Log(eSeverity.Error, "Failed to dispatch SOAP - {0}", e.Message);
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
		private WebPortResponse Dispatch(HttpRequestMessage request)
		{
			WebPortResponse output = WebPortResponse.Failed;

			m_ClientBusySection.Enter();

			try
			{
				ConfigureProxySettings();

				HttpResponseMessage response = m_Client.SendAsync(request).Result;

				if (response == null)
				{
					Log(eSeverity.Error, "{0} received null response. Is the port busy?", request.RequestUri);
				}
				else
				{
					if ((int)response.StatusCode >= 300)
						Log(eSeverity.Error, "{0} got response with error code {1}", request.RequestUri, response.StatusCode);

					output = new WebPortResponse
					{
						Success = (int)response.StatusCode < 300,
						StatusCode = (int)response.StatusCode,
						Data = response.Content.ReadAsByteArrayAsync().Result,
						Headers = response.Headers.ToDictionary(h => h.Key, h => h.Value.ToArray())
					};
				}
			}
			catch (AggregateException ae)
			{
				ae.Handle(x =>
				{
					if (x is TaskCanceledException)
						Log(eSeverity.Error, "{0} request timed out", request.RequestUri);
					else if (x is HttpRequestException)
					{
						Exception inner = x.GetBaseException();
						Log(eSeverity.Error, "{0} threw {1} - {2}", request.RequestUri, inner.GetType().Name, inner.Message);
					}
					else
						Log(eSeverity.Error, "{0} threw {1} - {2}", request.RequestUri, x.GetType().Name,
						    x.Message);

					return true;
				});
			}
			catch (HttpRequestException e)
			{
				Exception inner = e.GetBaseException();
				Log(eSeverity.Error, "{0} threw {1} - {2}", request.RequestUri, inner.GetType().Name, inner.Message);
			}
			catch (Exception e)
			{
				Log(eSeverity.Error, "{0} threw {1} - {2}", request.RequestUri, e.GetType().Name, e.Message);
			}
			finally
			{
				m_ClientBusySection.Leave();
			}

			SetLastRequestSucceeded(output.Success);
			PrintRx(output.ToString());

			return output;
		}

		private void ConfigureProxySettings()
		{
			WebProxy proxy = m_ClientHandler.Proxy as WebProxy;
			if (proxy == null)
				throw new InvalidOperationException("Client handler is configured with an unexpected proxy");

			proxy.Address = ProxyUri;
			proxy.Credentials = new NetworkCredential
			{
				UserName = ProxyUri == null ? null : ProxyUri.GetUserName(),
				Password = ProxyUri == null ? null : ProxyUri.GetPassword(),
			};

			m_ClientHandler.UseProxy = true;
		}

		private void AddHeader(HttpRequestMessage request, string header, string action)
		{
			AddHeader(request, header, new[] {action});
		}

		private static void AddHeader(HttpRequestMessage message, string header, IEnumerable<string> values)
		{
			switch (header)
			{
				case "Content-Type":
					message.Content.Headers.ContentType = new MediaTypeHeaderValue(values.Single());
					break;

				default:
					message.Headers.Add(header, values);
					break;
			}
		}

		#endregion
	}
}
#endif
