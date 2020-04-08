using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings;

namespace ICD.Connect.Protocol.Network.Ports.Web
{
	public abstract class AbstractWebPort<TSettings> : AbstractPort<TSettings>, IWebPort
		where TSettings : IWebPortSettings, new()
	{
		#region Properties

		/// <summary>
		/// Gets the URI configuration for the web port.
		/// </summary>
		public abstract IUriProperties UriProperties { get; }

		/// <summary>
		/// Gets the proxy configuration for the web port.
		/// </summary>
		public abstract IWebProxyProperties WebProxyProperties { get; }

		/// <summary>
		/// Gets/sets the base URI for requests.
		/// </summary>
		[CanBeNull]
		public abstract Uri Uri { get; set; }

		/// <summary>
		/// Gets/sets the proxy URI.
		/// </summary>
		[CanBeNull]
		public abstract Uri ProxyUri { get; set; }

		/// <summary>
		/// Gets/sets the proxy authentication method.
		/// </summary>
		public abstract eProxyAuthenticationMethod ProxyAuthenticationMethod { get; set; }

		/// <summary>
		/// Gets/sets the content type for the server to respond with.
		/// </summary>
		public abstract string Accept { get; set; }

		/// <summary>
		/// Returns true if currently waiting for a response from the server.
		/// </summary>
		public abstract bool Busy { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Sends a GET request to the server.
		/// </summary>
		/// <param name="relativeOrAbsoluteUri"></param>
		public WebPortResponse Get(string relativeOrAbsoluteUri)
		{
			return Get(relativeOrAbsoluteUri, new Dictionary<string, List<string>>());
		}

		/// <summary>
		/// Sends a GET request to the server.
		/// </summary>
		/// <param name="relativeOrAbsoluteUri"></param>
		/// <param name="headers"></param>
		public abstract WebPortResponse Get(string relativeOrAbsoluteUri, IDictionary<string, List<string>> headers);

		/// <summary>
		/// Sends a POST request to the server.
		/// </summary>
		/// <param name="relativeOrAbsoluteUri"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		public WebPortResponse Post(string relativeOrAbsoluteUri, byte[] data)
		{
			return Post(relativeOrAbsoluteUri, new Dictionary<string, List<string>>(), data);
		}

		/// <summary>
		/// Sends a POST request to the server.
		/// </summary>
		/// <param name="relativeOrAbsoluteUri"></param>
		/// <param name="headers"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		public abstract WebPortResponse Post(string relativeOrAbsoluteUri, Dictionary<string, List<string>> headers, byte[] data);

		/// <summary>
		/// Sends a SOAP request to the server.
		/// </summary>
		/// <param name="action"></param>
		/// <param name="content"></param>
		/// <returns></returns>
		public abstract WebPortResponse DispatchSoap(string action, string content);

		/// <summary>
		/// Applies the given device configuration properties to the port.
		/// </summary>
		/// <param name="properties"></param>
		public void ApplyDeviceConfiguration(IUriProperties properties)
		{
			if (properties == null)
				throw new ArgumentNullException("properties");

			// Port supersedes device configuration
			IUriProperties config = UriProperties.Superimpose(properties);

			ApplyConfiguration(config);
		}

		/// <summary>
		/// Applies the given device configuration properties to the port.
		/// </summary>
		/// <param name="properties"></param>
		public void ApplyDeviceConfiguration(IWebProxyProperties properties)
		{
			if (properties == null)
				throw new ArgumentNullException("properties");

			// Port supersedes device configuration
			IWebProxyProperties config = WebProxyProperties.Superimpose(properties);

			ApplyConfiguration(config);
		}

		/// <summary>
		/// Applies the configuration properties to the port.
		/// </summary>
		public void ApplyConfiguration()
		{
			ApplyConfiguration(UriProperties);
			ApplyConfiguration(WebProxyProperties);
		}

		/// <summary>
		/// Applies the given configuration properties to the port.
		/// </summary>
		/// <param name="properties"></param>
		public void ApplyConfiguration(IUriProperties properties)
		{
			if (properties == null)
				throw new ArgumentNullException("properties");

			IcdUriBuilder builder =
				Uri == null
					? new IcdUriBuilder()
					: new IcdUriBuilder(Uri);
			{
				if (!string.IsNullOrEmpty(properties.UriFragment))
					builder.Fragment = properties.UriFragment;

				if (!string.IsNullOrEmpty(properties.UriHost))
					builder.Host = properties.UriHost;

				if (!string.IsNullOrEmpty(properties.UriPassword))
					builder.Password = Uri.EscapeDataString(properties.UriPassword);

				if (!string.IsNullOrEmpty(properties.UriPath))
					builder.Path = properties.UriPath;

				// Set scheme before setting port
				if (!string.IsNullOrEmpty(properties.UriScheme))
					SetSchemeAndUpdatePort(builder, properties.UriScheme);

				if (properties.UriPort.HasValue)
					builder.Port = properties.UriPort.Value;

				if (!string.IsNullOrEmpty(properties.UriQuery))
					builder.Query = properties.UriQuery.TrimStart('?');

				if (!string.IsNullOrEmpty(properties.UriUsername))
					builder.UserName = Uri.EscapeDataString(properties.UriUsername);
			}

			try
			{
				Uri = builder.Uri.GetIsDefault() ? null : builder.Uri;
			}
			catch (UriFormatException e)
			{
				Logger.Log(eSeverity.Error, "Failed to set URI to {0} - {1}", builder.ToString(), e.Message);
				Uri = null;
			}
		}

		/// <summary>
		/// Applies the given configuration properties to the port.
		/// </summary>
		/// <param name="properties"></param>
		public void ApplyConfiguration(IWebProxyProperties properties)
		{
			if (properties == null)
				throw new ArgumentNullException("properties");

			IcdUriBuilder builder =
				ProxyUri == null
					? new IcdUriBuilder()
					: new IcdUriBuilder(ProxyUri);
			{
				if (!string.IsNullOrEmpty(properties.ProxyHost))
					builder.Host = properties.ProxyHost;

				if (!string.IsNullOrEmpty(properties.ProxyPassword))
					builder.Password = Uri.EscapeDataString(properties.ProxyPassword);

				// Set scheme before setting port
				if (!string.IsNullOrEmpty(properties.ProxyScheme))
					SetSchemeAndUpdatePort(builder, properties.ProxyScheme);

				if (properties.ProxyPort.HasValue)
					builder.Port = properties.ProxyPort.Value;

				if (!string.IsNullOrEmpty(properties.ProxyUsername))
					builder.UserName = Uri.EscapeDataString(properties.ProxyUsername);
			}

			try
			{
				ProxyUri = builder.Uri.GetIsDefault() ? null : builder.Uri;
			}
			catch (UriFormatException e)
			{
				Logger.Log(eSeverity.Error, "Failed to set Proxy URI to {0} - {1}", builder.ToString(), e.Message);
				ProxyUri = null;
			}
		}

		#endregion

		/// <summary>
		/// Sets the scheme and, if the port matches the old scheme, updates the port to match the new scheme.
		/// </summary>
		/// <param name="builder"></param>
		/// <param name="scheme"></param>
		private static void SetSchemeAndUpdatePort(IcdUriBuilder builder, string scheme)
		{
			ushort port;
			if (builder.Scheme != null && UriUtils.TryGetPortForScheme(builder.Scheme, out port) && port == builder.Port)
				builder.Port = 0;

			builder.Scheme = scheme;
		}

		#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(TSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Copy(UriProperties);
			settings.Copy(WebProxyProperties);
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			Uri = null;
			ProxyUri = null;
			ProxyAuthenticationMethod = eProxyAuthenticationMethod.None;

			UriProperties.ClearUriProperties();
			WebProxyProperties.ClearProxyProperties();

			ApplyConfiguration();
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(TSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			UriProperties.Copy(settings);
			WebProxyProperties.Copy(settings);
			
			ApplyConfiguration();
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
			addRow("Proxy URI", ProxyUri);
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
			WebPortResponse response = Get(path);
			return response.DataAsString;
		}

		/// <summary>
		/// Shim to perform a get request from the console.
		/// </summary>
		/// <param name="path"></param>
		private string ConsolePost(string path)
		{
			WebPortResponse response = Post(path, new byte[0]);
			return response.DataAsString;
		}

		#endregion
	}
}
