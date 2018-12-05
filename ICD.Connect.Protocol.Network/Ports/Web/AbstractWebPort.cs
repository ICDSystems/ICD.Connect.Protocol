using System;
using System.Collections.Generic;
using System.Text;
using ICD.Common.Properties;
using ICD.Common.Utils;
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
		/// The base URI for requests.
		/// </summary>
		[CanBeNull]
		public abstract Uri Uri { get; set; }

		/// <summary>
		/// Content type for the server to respond with. See HttpClient.Accept.
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
		/// <param name="localUrl"></param>
		/// <param name="headers"></param>
		/// <param name="response"></param>
		public abstract bool Get(string localUrl, IDictionary<string, List<string>> headers, out string response);

		/// <summary>
		/// Sends a GET request to the server.
		/// </summary>
		/// <param name="localUrl"></param>
		/// <param name="response"></param>
		public abstract bool Get(string localUrl, out string response);

		/// <summary>
		/// Sends a POST request to the server.
		/// </summary>
		/// <param name="localUrl"></param>
		/// <param name="data"></param>
		/// <param name="response"></param>
		/// <returns></returns>
		public abstract bool Post(string localUrl, byte[] data, out string response);

		/// <summary>
		/// Sends a POST request to the server.
		/// </summary>
		/// <param name="localUrl"></param>
		/// <param name="data"></param>
		/// <param name="encoding"></param>
		/// <param name="response"></param>
		/// <returns></returns>
		public abstract bool Post(string localUrl, string data, Encoding encoding, out string response);

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

			// Port supercedes device configuration
			IUriProperties config = UriProperties.Superimpose(properties);

			ApplyConfiguration(config);
		}

		/// <summary>
		/// Applies the configuration properties to the port.
		/// </summary>
		public void ApplyConfiguration()
		{
			ApplyConfiguration(UriProperties);
		}

		/// <summary>
		/// Applies the given configuration properties to the port.
		/// </summary>
		/// <param name="properties"></param>
		public void ApplyConfiguration(IUriProperties properties)
		{
			if (properties == null)
				throw new ArgumentNullException("properties");

			IcdUriBuilder builder = new IcdUriBuilder(Uri);
			{
				if (properties.UriFragment != null)
					builder.Fragment = properties.UriFragment;

				if (properties.UriHost != null)
					builder.Host = properties.UriHost;

				if (properties.UriPassword != null)
					builder.Password = properties.UriPassword;

				if (properties.UriPath != null)
					builder.Path = properties.UriPath;

				if (properties.UriPort.HasValue)
					builder.Port = properties.UriPort.Value;

				if (properties.UriQuery != null)
					builder.Query = properties.UriQuery;

				if (properties.UriScheme != null)
					builder.Scheme = properties.UriScheme;

				if (properties.UriUsername != null)
					builder.UserName = properties.UriUsername;
			}
			Uri = builder.Uri;
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(TSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Copy(UriProperties);
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			UriProperties.Clear();
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
		}

		#endregion
	}
}
