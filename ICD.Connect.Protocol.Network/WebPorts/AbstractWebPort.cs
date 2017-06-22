using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Protocol.Network.WebPorts
{
	/// <summary>
	/// Base class for web ports.
	/// </summary>
	public abstract class AbstractWebPort<T> : AbstractPort<T>, IWebPort
		where T : AbstractWebPortSettings, new()
	{
		protected const string SOAP_ACCEPT = "application/xml";
		protected const string SOAP_CONTENT_TYPE = "text/xml; charset=utf-8";
		protected const string SOAP_ACTION_HEADER = "SOAPAction";

		private bool m_LastRequestSucceeded;

		#region Properties

		/// <summary>
		/// The server address.
		/// </summary>
		public string Address { get; set; }

		/// <summary>
		/// The request protocol, i.e. http or https.
		/// </summary>
		protected abstract string Protocol { get; }

		/// <summary>
		/// Content type for the server to respond with. See HttpClient.Accept.
		/// </summary>
		public abstract string Accept { get; set; }

		/// <summary>
		/// Username for the server.
		/// </summary>
		public abstract string Username { get; set; }

		/// <summary>
		/// Password for the server.
		/// </summary>
		public abstract string Password { get; set; }

		/// <summary>
		/// Returns true if currently waiting for a response from the server.
		/// </summary>
		public abstract bool Busy { get; }

		#endregion

		/// <summary>
		/// Destructor.
		/// </summary>
		~AbstractWebPort()
		{
			Dispose();
		}

		#region Methods

		/// <summary>
		/// Sends a GET request to the server.
		/// </summary>
		/// <param name="localUrl"></param>
		public abstract string Get(string localUrl);

		/// <summary>
		/// Sends a POST request to the server.
		/// </summary>
		/// <param name="localUrl"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		public abstract string Post(string localUrl, byte[] data);

		/// <summary>
		/// Sends a SOAP request to the server.
		/// </summary>
		/// <param name="action"></param>
		/// <param name="content"></param>
		/// <returns></returns>
		public abstract string DispatchSoap(string action, string content);

		/// <summary>
		/// Builds a request url from the given path.
		/// e.g.
		///		"Test/Path"
		/// May result in
		///		https://10.3.14.15/Test/Path
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		[PublicAPI]
		public string GetRequestUrl(string data)
		{
			string output = Address;

			if (!output.Contains("://"))
				output = string.Format("{0}://{1}", Protocol, output);

			if (!string.IsNullOrEmpty(data))
				output = string.Format("{0}/{1}", output, data);

			return output;
		}

		#endregion

		#region Private Methods

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
		protected string Request(string content, Func<string, string> requestMethod)
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
		protected override void CopySettingsFinal(T settings)
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
			Address = null;
			Password = null;
			Username = null;
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(T settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			Address = settings.Address;
			Password = settings.Password;
			Username = settings.Username;
			Accept = settings.Accept;
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
			addRow("Protocol", Protocol);
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
