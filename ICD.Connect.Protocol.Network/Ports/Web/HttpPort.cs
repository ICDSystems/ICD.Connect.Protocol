using System.Collections.Generic;
using System.Text;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings;

namespace ICD.Connect.Protocol.Network.Ports.Web
{
	/// <summary>
	/// Allows for communication with a HTTP service.
	/// </summary>
	public sealed partial class HttpPort : AbstractPort<HttpPortSettings>, IWebPort
	{
		private const string SOAP_ACCEPT = "application/xml";
		private const string SOAP_CONTENT_TYPE = "text/xml; charset=utf-8";
		private const string SOAP_ACTION_HEADER = "SOAPAction";

		private readonly UriProperties m_UriProperties = new UriProperties();

		private bool m_LastRequestSucceeded;

		#region Properties

		/// <summary>
		/// Gets the configured URI properties.
		/// </summary>
		public UriProperties UriProperties { get { return m_UriProperties; } }

		#endregion

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
			IcdUriBuilder builder = new IcdUriBuilder
			{
				Fragment = m_UriProperties.UriFragment,
				Host = m_UriProperties.UriHost,
				Password = m_UriProperties.UriPassword,
				Path = localAddress,
				Port = m_UriProperties.UriPort,
				Query = m_UriProperties.UriQuery,
				Scheme = m_UriProperties.UriScheme,
				UserName = m_UriProperties.UriUsername
			};

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
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(HttpPortSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Copy(m_UriProperties);
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			m_UriProperties.Clear();
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(HttpPortSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			m_UriProperties.Copy(settings);
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

			addRow("URI", m_UriProperties.GetAddressFromUri());
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
