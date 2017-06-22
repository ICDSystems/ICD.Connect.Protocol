using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharp.Net.Https;
using ICD.Common.Properties;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;
using RequestType = Crestron.SimplSharp.Net.Https.RequestType;

#if SIMPLSHARP

namespace ICD.Connect.Protocol.Network.WebPorts.Https
{
	public sealed partial class HttpsPort
	{
		private readonly HttpsClient m_Client;
		private readonly SafeCriticalSection m_ClientBusySection;

		#region Properties

		/// <summary>
		/// Content type for the server to respond with. See HttpClient.Accept.
		/// </summary>
		public override string Accept { get { return m_Client.Accept; } set { m_Client.Accept = value; } }

		/// <summary>
		/// Username for the server.
		/// </summary>
		public override string Username { get { return m_Client.UserName; } set { m_Client.UserName = value; } }

		/// <summary>
		/// Password for the server.
		/// </summary>
		public override string Password { get { return m_Client.Password; } set { m_Client.Password = value; } }

		/// <summary>
		/// Returns true if currently waiting for a response from the server.
		/// </summary>
		public override bool Busy { get { return m_Client.ProcessBusy; } }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public HttpsPort()
		{
			m_Client = new HttpsClient
			{
				KeepAlive = false,
				TimeoutEnabled = true,
				Timeout = 2,
				HostVerification = false,
				PeerVerification = false
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
		/// Loads the SSL certificate from the given path.
		/// </summary>
		/// <param name="fullPath"></param>
		/// <param name="password"></param>
		/// <param name="type"></param>
		[PublicAPI]
		public void LoadClientCertificateFinal(string fullPath, string password, eCertificateType type)
		{
			m_ClientBusySection.Enter();

			try
			{
				m_Client.SetClientCertificate(fullPath, password, (HttpsClient.ClientCertificateType)type);
			}
			catch (FileNotFoundException)
			{
				Logger.AddEntry(eSeverity.Error, "SSL Certificate does not exist: {0}", fullPath);
			}
			finally
			{
				m_ClientBusySection.Leave();
			}
		}

		/// <summary>
		/// Sends a GET request to the server.
		/// </summary>
		/// <param name="localUrl"></param>
		public override string Get(string localUrl)
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
		public override string Post(string localUrl, byte[] data)
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
		public override string DispatchSoap(string action, string content)
		{
			m_ClientBusySection.Enter();

			try
			{
				m_Client.Accept = SOAP_ACCEPT;
				m_Client.IncludeHeaders = false;

				HttpsClientRequest request = new HttpsClientRequest
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

		/// <summary>
		/// Dispatches the request and returns the result.
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		public string Dispatch(HttpsClientRequest request)
		{
			m_ClientBusySection.Enter();

			try
			{
				HttpsClientResponse response = m_Client.Dispatch(request);

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

		#endregion
	}
}

#endif
