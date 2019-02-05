﻿#if STANDARD
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ICD.Common.Utils;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Protocol.Network.Settings;

namespace ICD.Connect.Protocol.Network.Ports.Web
{
	public sealed partial class HttpPort
	{
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
				ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
			};

			m_Client = new HttpClient(m_ClientHandler)
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

			m_ClientHandler.Dispose();
		}

		/// <summary>
		/// Sends a GET request to the server.
		/// </summary>
		/// <param name="localUrl"></param>
		/// <param name="response"></param>
		public override bool Get(string localUrl, out string response)
		{
			return Get(localUrl, new Dictionary<string, List<string>>(), out response);
		}

		/// <summary>
		/// Sends a GET request to the server.
		/// </summary>
		/// <param name="localUrl"></param>
		/// <param name="headers"></param>
		/// <param name="response"></param>
		public override bool Get(string localUrl, IDictionary<string, List<string>> headers, out string response)
		{
			bool success;

			m_ClientBusySection.Enter();

			try
			{
				Uri uri = new Uri(m_UriProperties.GetUri(), localUrl);
				PrintTx(uri.AbsolutePath);

				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
				foreach (KeyValuePair<string, List<string>> header in headers)
				{
					request.Headers.Add(header.Key, header.Value);
				}
				success = Dispatch(request, out response);
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
		public override bool Post(string localUrl, byte[] data, out string response)
		{
			bool success;

			m_ClientBusySection.Enter();

			try
			{
				Uri uri = new Uri(m_UriProperties.GetUri(), localUrl);
				PrintTx(uri.AbsolutePath);

				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri)
				{
					Content = new ByteArrayContent(data)
				};

				success = Dispatch(request, out response);
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
		public override bool DispatchSoap(string action, string content, out string response)
		{
			PrintTx(action);

			Accept = SOAP_ACCEPT;

			bool success;

			m_ClientBusySection.Enter();

			try
			{
				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, m_UriProperties.GetUri())
				{
					Content = new StringContent(content, Encoding.ASCII, SOAP_CONTENT_TYPE)
				};

				request.Headers.Add(SOAP_ACTION_HEADER, action);

				success = Dispatch(request, out response);
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
		/// Dispatches the request and returns the result.
		/// </summary>
		/// <param name="request"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		private bool Dispatch(HttpRequestMessage request, out string result)
		{
			result = null;

			m_ClientBusySection.Enter();

			try
			{
				HttpResponseMessage response = m_Client.SendAsync(request).Result;

				if (response == null)
				{
					Log(eSeverity.Error, "{0} received null response. Is the port busy?", request.RequestUri);
					return false;
				}

				result = response.Content.ReadAsStringAsync().Result;

				if ((int)response.StatusCode < 300)
					return true;

				Log(eSeverity.Error, "{0} got response with error code {1}", request.RequestUri, response.StatusCode);
				return false;
			}
			catch (AggregateException ae)
			{
				ae.Handle(x =>
				          {
					          if (x is TaskCanceledException)
					          {
						          Log(eSeverity.Error, "{0} request timed out", request.RequestUri);
								  return true;
					          }

					          return false;
				          });

				return false;
			}
			finally
			{
				m_ClientBusySection.Leave();
			}
		}

		#endregion
	}
}
#endif