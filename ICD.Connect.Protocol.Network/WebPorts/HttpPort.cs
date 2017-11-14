using System.Collections.Generic;
using System.Text;
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
	/// Allows for communication with a HTTP service.
	/// </summary>
	public sealed partial class HttpPort : AbstractPort<HttpPortSettings>, IWebPort
	{
		private const string DEFAULT_ACCEPT = "text/html";

		private const string SOAP_ACCEPT = "application/xml";
		private const string SOAP_CONTENT_TYPE = "text/xml; charset=utf-8";
		private const string SOAP_ACTION_HEADER = "SOAPAction";

		private bool m_LastRequestSucceeded;

		#region Methods

		/// <summary>
		/// Sends a POST request to the server. Assumes data is ASCII.
		/// </summary>
		/// <param name="localUrl"></param>
		/// <param name="data"></param>
		/// <param name="response"></param>
		/// <returns></returns>
		[PublicAPI]
		public bool Post(string localUrl, string data, out string response)
		{
			return Post(localUrl, data, new ASCIIEncoding(), out response);
		}

		/// <summary>
		/// Sends a POST request to the server using the given encoding for data.
		/// </summary>
		/// <param name="localUrl"></param>
		/// <param name="data"></param>
		/// <param name="encoding"></param>
		/// <param name="response"></param>
		/// <returns></returns>
		[PublicAPI]
		public bool Post(string localUrl, string data, Encoding encoding, out string response)
		{
			byte[] bytes = encoding.GetBytes(data);
			return Post(localUrl, bytes, out response);
		}

		#endregion

		#region Private Methods

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
			string output = GetAddressWithProtocol();

			if (string.IsNullOrEmpty(localAddress))
				return output;

			// Avoid doubling up the trailing slash
			if (localAddress.StartsWith("/"))
				localAddress = localAddress.Substring(1);

			// Append the local address to the base address
			return string.Format("{0}{1}", output, localAddress);
		}

		private string GetAddressWithProtocol()
		{
			string output = Address;

			// Ensure the address starts with a protocol
			if (!output.Contains("://"))
				output = string.Format("http://{0}", output);

			// Ensure the address ends with a trailing slash
			if (!output.EndsWith("/"))
				output = string.Format("{0}/", output);

			return output;
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

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(HttpPortSettings settings)
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

			Accept = DEFAULT_ACCEPT;
			Address = null;
			Password = null;
			Username = null;
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(HttpPortSettings settings, IDeviceFactory factory)
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
			
			yield return new GenericConsoleCommand<string>("Get", "Performs a request at the given path", a => ConsoleGet(a));
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
		/// <param name="path"></param>
		private string ConsoleGet(string path)
		{
			string output;
			Get(path, out output);
			return output;
		}

		#endregion
	}
}
