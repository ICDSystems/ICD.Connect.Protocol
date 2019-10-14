﻿using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
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
		public abstract IProxyProperties ProxyProperties { get; }

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
		/// <param name="response"></param>
		public bool Get(string relativeOrAbsoluteUri, out string response)
		{
			return Get(relativeOrAbsoluteUri, new Dictionary<string, List<string>>(), out response);
		}

		/// <summary>
		/// Sends a GET request to the server.
		/// </summary>
		/// <param name="relativeOrAbsoluteUri"></param>
		/// <param name="headers"></param>
		/// <param name="response"></param>
		public abstract bool Get(string relativeOrAbsoluteUri, IDictionary<string, List<string>> headers, out string response);

		/// <summary>
		/// Sends a POST request to the server.
		/// </summary>
		/// <param name="relativeOrAbsoluteUri"></param>
		/// <param name="data"></param>
		/// <param name="response"></param>
		/// <returns></returns>
		public bool Post(string relativeOrAbsoluteUri, byte[] data, out string response)
		{
			return Post(relativeOrAbsoluteUri, new Dictionary<string, List<string>>(), data, out response);
		}

		/// <summary>
		/// Sends a POST request to the server.
		/// </summary>
		/// <param name="relativeOrAbsoluteUri"></param>
		/// <param name="headers"></param>
		/// <param name="data"></param>
		/// <param name="response"></param>
		/// <returns></returns>
		public abstract bool Post(string relativeOrAbsoluteUri, Dictionary<string, List<string>> headers, byte[] data, out string response);

		/// <summary>
		/// Sends a SOAP request to the server.
		/// </summary>
		/// <param name="action"></param>
		/// <param name="content"></param>
		/// <param name="response"></param>
		/// <returns></returns>
		public abstract bool DispatchSoap(string action, string content, out string response);

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
		public void ApplyDeviceConfiguration(IProxyProperties properties)
		{
			if (properties == null)
				throw new ArgumentNullException("properties");

			// Port supersedes device configuration
			IProxyProperties config = ProxyProperties.Superimpose(properties);

			ApplyConfiguration(config);
		}

		/// <summary>
		/// Applies the configuration properties to the port.
		/// </summary>
		public void ApplyConfiguration()
		{
			ApplyConfiguration(UriProperties);
			ApplyConfiguration(ProxyProperties);
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
				if (properties.UriFragment != null)
					builder.Fragment = properties.UriFragment;

				if (properties.UriHost != null)
					builder.Host = properties.UriHost;

				if (properties.UriPassword != null)
					builder.Password = Uri.EscapeDataString(properties.UriPassword);

				if (properties.UriPath != null)
					builder.Path = properties.UriPath;

				// Set scheme before setting port
				if (properties.UriScheme != null)
					SetSchemeAndUpdatePort(builder, properties.UriScheme);

				if (properties.UriPort.HasValue)
					builder.Port = properties.UriPort.Value;

				if (properties.UriQuery != null)
					builder.Query = properties.UriQuery.TrimStart('?');

				if (properties.UriUsername != null)
					builder.UserName = Uri.EscapeDataString(properties.UriUsername);
			}

			try
			{
				Uri = builder.Uri;
			}
			catch (UriFormatException e)
			{
				Log(eSeverity.Error, "Failed to set URI - {0}", e.Message);
				Uri = null;
			}
		}

		/// <summary>
		/// Applies the given configuration properties to the port.
		/// </summary>
		/// <param name="properties"></param>
		public void ApplyConfiguration(IProxyProperties properties)
		{
			if (properties == null)
				throw new ArgumentNullException("properties");

			IcdUriBuilder builder =
				ProxyUri == null
					? new IcdUriBuilder()
					: new IcdUriBuilder(ProxyUri);
			{
				if (properties.ProxyHost != null)
					builder.Host = properties.ProxyHost;

				if (properties.ProxyPassword != null)
					builder.Password = Uri.EscapeDataString(properties.ProxyPassword);

				// Set scheme before setting port
				if (properties.ProxyScheme != null)
					SetSchemeAndUpdatePort(builder, properties.ProxyScheme);

				if (properties.ProxyPort.HasValue)
					builder.Port = properties.ProxyPort.Value;

				if (properties.ProxyUsername != null)
					builder.UserName = Uri.EscapeDataString(properties.ProxyUsername);
			}

			try
			{
				ProxyUri = builder.Uri;
			}
			catch (UriFormatException e)
			{
				Log(eSeverity.Error, "Failed to set Proxy URI - {0}", e.Message);
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
			settings.Copy(ProxyProperties);
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
			ProxyProperties.ClearProxyProperties();

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
			ProxyProperties.Copy(settings);
			
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
			Post(path, new byte[0], out output);
			return output;
		}

		#endregion
	}
}
