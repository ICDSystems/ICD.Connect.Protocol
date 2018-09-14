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

namespace ICD.Connect.Protocol.Network.WebPorts
{
	public sealed partial class HttpPort
	{
		private readonly HttpClient m_Client;
		private readonly HttpClientHandler m_ClientHandler;
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
		public bool Get(string localUrl, out string response)
		{
			return Get(localUrl, out response, new Dictionary<string, List<string>>());
		}

		/// <summary>
		/// Sends a GET request to the server.
		/// </summary>
		/// <param name="localUrl"></param>
		/// <param name="response"></param>
		/// <param name="headers"></param>
		public bool Get(string localUrl, out string response, Dictionary<string, List<string>> headers)
		{
			bool success;

			m_ClientBusySection.Enter();

			try
			{
				Uri uri = new Uri(new Uri(GetAddressWithProtocol()), localUrl);
				PrintTx(uri.AbsolutePath);

				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri){};
			    foreach (var header in headers)
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
		public bool Post(string localUrl, byte[] data, out string response)
		{
			bool success;

			m_ClientBusySection.Enter();

			try
			{
				Uri uri = new Uri(new Uri(GetAddressWithProtocol()), localUrl);
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
		public bool DispatchSoap(string action, string content, out string response)
		{
			PrintTx(action);

			Accept = SOAP_ACCEPT;

			bool success;

			m_ClientBusySection.Enter();

			try
			{
				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, GetAddressWithProtocol())
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

		#region Private Methods

		private void SetAuth(string username, string password)
		{
			string auth = string.Format("{0}:{1}", username, password);
			byte[] byteArray = Encoding.ASCII.GetBytes(auth);
			m_Client.DefaultRequestHeaders.Authorization =
				new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
		}

		#endregion
	}
}
#endif
