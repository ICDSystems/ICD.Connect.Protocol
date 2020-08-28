using System.Linq;
using ICD.Connect.API.Commands;
#if SIMPLSHARP
using System;
﻿using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using Crestron.SimplSharp.Net;
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharp.Net.Https;
using ICD.Common.Utils;
using ContentSource = Crestron.SimplSharp.Net.Https.ContentSource;
using RequestType = Crestron.SimplSharp.Net.Https.RequestType;

namespace ICD.Connect.Protocol.Network.Ports.Web
{
	public sealed partial class HttpPort
	{
		private const string SOAP_CONTENT_TYPE = "text/xml; charset=utf-8";

		private readonly HttpsClient m_HttpsClient;
		private readonly SafeCriticalSection m_ClientBusySection;

		#region Properties

		/// <summary>
		/// Content type for the server to respond with. See HttpClient.Accept.
		/// </summary>
		public override string Accept
		{
			get { return m_HttpsClient.Accept; }
			set { m_HttpsClient.Accept = string.IsNullOrEmpty(value) ? "*/*" : value; }
		}

		/// <summary>
		/// Returns true if currently waiting for a response from the server.
		/// </summary>
		public override bool Busy { get { return m_HttpsClient.ProcessBusy; } }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public HttpPort()
		{
			m_HttpsClient = new HttpsClient
			{
				KeepAlive = true,
				Accept = "*/*",
				TimeoutEnabled = true,
				Timeout = 60,
				HostVerification = false,
				PeerVerification = false,
				UserAgent = "Crestron SimplSharp HTTPS Client"
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
				m_HttpsClient.Abort();
			}
			catch
			{
			}
		}

		/// <summary>
		/// Sends a GET request to the server.
		/// </summary>
		/// <param name="relativeOrAbsoluteUri"></param>
		/// <param name="headers"></param>
		public override WebPortResponse Get(string relativeOrAbsoluteUri, IDictionary<string, List<string>> headers)
		{
			m_ClientBusySection.Enter();

			try
			{
				string url = GetRequestUrl(relativeOrAbsoluteUri);
				PrintTx(url);

				HttpsClientRequest request = new HttpsClientRequest
				{
					KeepAlive = m_HttpsClient.KeepAlive,
					RequestType = RequestType.Get
				};

				request.Url.Parse(url);
				request.Header.SetHeaderValue("Accept", Accept);
				request.Header.SetHeaderValue("Expect", "");

				foreach (KeyValuePair<string, List<string>> header in headers)
					request.Header.SetHeaderValue(header.Key, string.Join(";", header.Value.ToArray()));

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
		/// <param name="dictionary"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		public override WebPortResponse Post(string relativeOrAbsoluteUri, Dictionary<string, List<string>> dictionary, byte[] data)
		{
			m_ClientBusySection.Enter();

			try
			{
				string url = GetRequestUrl(relativeOrAbsoluteUri);
				PrintTx(url);

				HttpsClientRequest request = new HttpsClientRequest
				{
					KeepAlive = m_HttpsClient.KeepAlive,
					ContentBytes = data,
					ContentSource = ContentSource.ContentBytes,
					RequestType = RequestType.Post
				};

				request.Url.Parse(url);
				request.Header.SetHeaderValue("Accept", Accept);
				request.Header.SetHeaderValue("Expect", "");
				request.Header.SetHeaderValue("User-Agent", m_HttpsClient.UserAgent);

				return Dispatch(request);
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
				string url = GetRequestUrl(relativeOrAbsoluteUri);
				PrintTx(url);

				HttpsClientRequest request = new HttpsClientRequest
				{
					KeepAlive = m_HttpsClient.KeepAlive,
					ContentBytes = data,
					ContentSource = ContentSource.ContentBytes,
					RequestType = RequestType.Patch
				};

				request.Url.Parse(url);
				request.Header.SetHeaderValue("Accept", Accept);
				request.Header.SetHeaderValue("Expect", "");
				request.Header.SetHeaderValue("User-Agent", m_HttpsClient.UserAgent);

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
			m_HttpsClient.IncludeHeaders = false;

			m_ClientBusySection.Enter();

			try
			{
				string url = Uri == null ? null : Uri.ToString();
				UrlParser urlParser = new UrlParser(url);

				HttpsClientRequest request = new HttpsClientRequest
				{
					RequestType = RequestType.Post,
					Url = urlParser,
					Header = {ContentType = SOAP_CONTENT_TYPE},
					ContentString = content
				};
				request.Header.SetHeaderValue(SOAP_ACTION_HEADER, action);
				request.Header.SetHeaderValue("Expect", "");

				return Dispatch(request);
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
			WebPortResponse output = WebPortResponse.Failed;

			m_ClientBusySection.Enter();

			try
			{
				ConfigureProxySettings();

				HttpsClientResponse response = m_HttpsClient.Dispatch(request);

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
						Success = response.Code < 300,
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

			SetLastRequestSucceeded(output.Success);
			PrintRx(output.DataAsString);

			return output;
		}

		private void ConfigureProxySettings()
		{
			string urlOrHost =
				ProxyUri == null || ProxyUri.GetIsDefault()
					? null
					: ProxyUri.ToString();

			if (urlOrHost == null)
			{
				m_HttpsClient.Proxy = null;
				return;
			}

			ProxySettings settings = m_HttpsClient.Proxy ?? (m_HttpsClient.Proxy = new ProxySettings(urlOrHost));

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

			yield return new ConsoleCommand("EnableVerboseMode", "Enables Verbose mode on the Crestron HttpsClient", () => m_HttpsClient.Verbose = true);
			yield return new ConsoleCommand("DisableVerboseMode", "Disables Verbose mode on the Crestron HttpsClient", () => m_HttpsClient.Verbose = false);
		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}

#endif
