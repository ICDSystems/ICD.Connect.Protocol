using ICD.Common.Services.Logging;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace ICD.Connect.Protocol.Network.WebPorts.CoreHttpPort
{
	public sealed class CoreHttpPort : AbstractPort<CoreHttpPortSettings>, IWebPort
    {
		private const string DEFAULT_ACCEPT = "text/html";

		private const string SOAP_ACCEPT = "application/xml";
		private const string SOAP_CONTENT_TYPE = "text/xml; charset=utf-8";
		private const string SOAP_ACTION_HEADER = "SOAPAction";

		private readonly HttpClient m_Client;
		private readonly SafeCriticalSection m_ClientBusySection;

		private bool m_LastRequestSucceeded;
		private string m_Username;
		private string m_Password;

		#region Properties

		/// <summary>
		/// The server address.
		/// </summary>
		public string Address { get { return m_Client.BaseAddress.AbsoluteUri; } set { m_Client.BaseAddress = new Uri(value); } }

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
		public CoreHttpPort()
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
				string url = GetRequestUrl(localUrl);
				return Request(url, s => m_Client.GetAsync(s).Result.Content.ReadAsStringAsync().Result);
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
				HttpContent content = new ByteArrayContent(data);

				return Request(url, s => m_Client.PostAsync(s, content).Result.Content.ReadAsStringAsync().Result);
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

				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, Address);
				request.Content = new StringContent(content, Encoding.ASCII, SOAP_CONTENT_TYPE);
				request.Headers.Add(SOAP_ACTION_HEADER, action);

				return Request(content, s => Dispatch(request));
			}
			finally
			{
				m_ClientBusySection.Leave();
			}
		}

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

			if (!string.IsNullOrEmpty(data))
				output = string.Format("{0}/{1}", output, data);

			return output;
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
					return null;
				}

				if ((int)response.StatusCode < 300)
					return response.Content.ReadAsStringAsync().Result;

				Logger.AddEntry(eSeverity.Error, "{0} {1} got response with error code {2}", this, request.RequestUri, response.StatusCode);
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

		/// <summary>
		/// Updates the IsOnline status and raises events.
		/// </summary>
		/// <param name="succeeded"></param>
		private void SetLastRequestSucceeded(bool succeeded)
		{
			m_LastRequestSucceeded = succeeded;
			UpdateCachedOnlineStatus();
		}

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return m_LastRequestSucceeded;
		}

		/// <summary>
		/// Common logic for sending a request to the port.
		/// </summary>
		/// <param name="content"></param>
		/// <param name="requestMethod"></param>
		/// <returns></returns>
		private string Request(string content, Func<string, string> requestMethod)
		{
			string output;

			try
			{
				PrintTx(content);
				output = requestMethod(content);
				SetLastRequestSucceeded(true);
			}
			catch (Exception)
			{
				SetLastRequestSucceeded(false);
				throw;
			}

			PrintRx(output);
			return output;
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(CoreHttpPortSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Address = Address;
			settings.Accept = Accept;
			settings.Password = Password;
			settings.Username = Username;
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			Accept = null;
			Address = DEFAULT_ACCEPT;
			Password = null;
			Username = null;
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(CoreHttpPortSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			Address = settings.Address;
			Password = settings.Password;
			Username = settings.Username;
			Accept = settings.Accept ?? DEFAULT_ACCEPT;
		}

		#endregion

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Address", Address);
			addRow("Accept", Accept);
			addRow("Username", Username);
			addRow("Password", StringUtils.PasswordFormat(Password));
			addRow("Busy", Busy);
		}

		/// <summary>
		/// Gets the console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<string>("SetAddress", "Sets the address for requests", s => Address = s);
			yield return new GenericConsoleCommand<string>("SetAccept", "Sets the accept for requests", s => Accept = s);
			yield return new GenericConsoleCommand<string>("SetUsername", "Sets the username for requests", s => Username = s);
			yield return new GenericConsoleCommand<string>("SetPassword", "Sets the password for requests", s => Password = s);

			yield return new ParamsConsoleCommand("Get", "Performs a request at the given path", a => ConsoleGet(a));
		}

		/// <summary>
		/// Shim to avoid "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		/// <summary>
		/// Shim to perform a get request from the console.
		/// </summary>
		/// <param name="paths"></param>
		private void ConsoleGet(params string[] paths)
		{
			paths.ForEach(p => Get(p));
		}

		#endregion
	}
}
