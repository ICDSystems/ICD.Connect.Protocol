using System;
using System.Collections.Generic;
using System.Text;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Settings;

namespace ICD.Connect.Protocol.Network.Ports.Web
{
	/// <summary>
	/// Allows for communication with a HTTP service.
	/// </summary>
	public sealed partial class HttpPort : AbstractWebPort<HttpPortSettings>
	{
		private const string SOAP_ACCEPT = "application/xml";
		private const string SOAP_ACTION_HEADER = "SOAPAction";

		private readonly UriProperties m_UriProperties = new UriProperties();

		private bool m_LastRequestSucceeded;

		#region Properties

		/// <summary>
		/// Gets the configured URI properties.
		/// </summary>
		public override IUriProperties UriProperties
		{
			get { return m_UriProperties; }
		}

		/// <summary>
		/// The base URI for requests.
		/// </summary>
		public override Uri Uri { get; set; }

		#endregion

		#region Methods

		/// <summary>
		/// Sends a POST request to the server. Assumes data is ASCII.
		/// </summary>
		/// <param name="relativeOrAbsoluteUri"></param>
		/// <param name="data"></param>
		/// <param name="response"></param>
		/// <returns></returns>
		[PublicAPI]
		public bool Post(string relativeOrAbsoluteUri, string data, out string response)
		{
			return Post(relativeOrAbsoluteUri, data, new ASCIIEncoding(), out response);
		}

		/// <summary>
		/// Sends a GET request to the server.
		/// </summary>
		/// <param name="relativeOrAbsoluteUri"></param>
		/// <param name="response"></param>
		public override bool Get(string relativeOrAbsoluteUri, out string response)
		{
			return Get(relativeOrAbsoluteUri, new Dictionary<string, List<string>>(), out response);
		}

		/// <summary>
		/// Sends a GET request to the server.
		/// </summary>
		/// <param name="relativeOrAbsoluteUri"></param>
		/// <param name="headers"></param>
		/// <param name="response"></param>
		public override bool Get(string relativeOrAbsoluteUri, IDictionary<string, List<string>> headers, out string response)
		{
			return Get(relativeOrAbsoluteUri, headers, null, out response);
		}

		/// <summary>
		/// Sends a POST request to the server using the given encoding for data.
		/// </summary>
		/// <param name="relativeOrAbsoluteUri"></param>
		/// <param name="data"></param>
		/// <param name="encoding"></param>
		/// <param name="response"></param>
		/// <returns></returns>
		[PublicAPI]
		public override bool Post(string relativeOrAbsoluteUri, string data, Encoding encoding, out string response)
		{
			byte[] bytes = encoding.GetBytes(data);
			return Post(relativeOrAbsoluteUri, bytes, out response);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Builds a request url from the given relative or absolute uri.
		/// e.g.
		///		"Test/Path"
		/// May result in
		///		https://10.3.14.15/Test/Path
		/// </summary>
		/// <param name="relativeOrAbsolute"></param>
		/// <returns></returns>
		private string GetRequestUrl(string relativeOrAbsolute)
		{
			IcdUriBuilder builder = new IcdUriBuilder(Uri);

			if (Uri.IsWellFormedUriString(relativeOrAbsolute, UriKind.Absolute))
				builder = new IcdUriBuilder(relativeOrAbsolute);
			else
				builder.AppendPath(relativeOrAbsolute);

#if SIMPLSHARP
			// Crestron tries to strip out encoded spaces (and possibly other encodings?) from the path
			// so we doubly-escape to prevent this from happening.
			builder.Path = string.IsNullOrEmpty(builder.Path)
				? builder.Path
				: builder.Path.Replace("%", "%25");
#endif

			return builder.ToString();
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
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			ApplyConfiguration();
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(HttpPortSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			ApplyConfiguration();

			// Reset the online state
			SetLastRequestSucceeded(true);
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

			addRow("URI", Uri);
			addRow("Accept", Accept);
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

			yield return new GenericConsoleCommand<string>("SetAccept", "Sets the accept for requests", s => Accept = s);
			yield return new GenericConsoleCommand<string>("Get", "Performs a request at the given path", a => ConsoleGet(a));
			yield return new GenericConsoleCommand<string>("Post", "Performs a request at the given path", a => ConsolePost(a));
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

		/// <summary>
		/// Shim to perform a get request from the console.
		/// </summary>
		/// <param name="path"></param>
		private string ConsolePost(string path)
		{
			string output;
			Post(path, "", out output);
			return output;
		}

		#endregion
	}
}
