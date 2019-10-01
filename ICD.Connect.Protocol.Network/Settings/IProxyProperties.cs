using System;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.Network.Ports.Web;

namespace ICD.Connect.Protocol.Network.Settings
{
	public interface IProxyProperties
	{
		/// <summary>
		/// Gets/sets the configurable proxy username.
		/// </summary>
		string ProxyUsername { get; set; }

		/// <summary>
		/// Gets/sets the configurable proxy password.
		/// </summary>
		string ProxyPassword { get; set; }

		/// <summary>
		/// Gets/sets the configurable proxy host.
		/// </summary>
		string ProxyHost { get; set; }

		/// <summary>
		/// Gets/sets the configurable proxy port.
		/// </summary>
		ushort? ProxyPort { get; set; }

		/// <summary>
		/// Gets/sets the configurable proxy scheme.
		/// </summary>
		string ProxyScheme { get; set; }

		/// <summary>
		/// Gets/sets the configurable proxy authentication method.
		/// </summary>
		eProxyAuthenticationMethod? ProxyAuthenticationMethod { get; set; }

		/// <summary>
		/// Clears the configured values.
		/// </summary>
		void ClearProxyProperties();
	}

	public static class ProxyPropertiesExtensions
	{
		/// <summary>
		/// Copies the configured properties from the given Network Properties instance.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="other"></param>
		public static void Copy(this IProxyProperties extends, IProxyProperties other)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (other == null)
				throw new ArgumentNullException("other");

			extends.ProxyUsername = other.ProxyUsername;
			extends.ProxyPassword = other.ProxyPassword;
			extends.ProxyHost = other.ProxyHost;
			extends.ProxyPort = other.ProxyPort;
			extends.ProxyScheme = other.ProxyScheme;
			extends.ProxyAuthenticationMethod = other.ProxyAuthenticationMethod;
		}

		/// <summary>
		/// Updates the Proxy Properties instance where values are not already configured.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="username"></param>
		/// <param name="password"></param>
		/// <param name="host"></param>
		/// <param name="port"></param>
		/// <param name="scheme"></param>
		/// <param name="authMethod"></param>
		public static void ApplyDefaultValues(this IProxyProperties extends, string username, string password, string host,
		                                      ushort? port, string scheme, eProxyAuthenticationMethod? authMethod)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (extends.ProxyUsername == null)
				extends.ProxyUsername = username;

			if (extends.ProxyPassword == null)
				extends.ProxyPassword = password;

			if (extends.ProxyHost == null)
				extends.ProxyHost = host;

			if (extends.ProxyPort == null)
				extends.ProxyPort = port;

			if (extends.ProxyScheme == null)
				extends.ProxyScheme = scheme;

			if (extends.ProxyAuthenticationMethod == null)
				extends.ProxyAuthenticationMethod = authMethod;
		}

		/// <summary>
		/// Creates a new properties instance, applying this instance over the top of the other instance.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="other"></param>
		/// <returns></returns>
		public static IProxyProperties Superimpose(this IProxyProperties extends, IProxyProperties other)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (other == null)
				throw new ArgumentNullException("other");

			ProxyProperties output = new ProxyProperties();

			output.Copy(extends);
			output.ApplyDefaultValues(other.ProxyUsername,
			                          other.ProxyPassword,
			                          other.ProxyHost,
			                          other.ProxyPort,
			                          other.ProxyScheme,
			                          other.ProxyAuthenticationMethod);

			return output;
		}

		/// <summary>
		/// Builds a URI from the configured URI properties.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static Uri GetUri(this IProxyProperties extends)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			IcdUriBuilder builder = new IcdUriBuilder
			{
				Host = extends.ProxyHost,
				Password = extends.ProxyPassword == null ? null : Uri.EscapeDataString(extends.ProxyPassword),
				Port = extends.ProxyPort ?? 0,
				Scheme = extends.ProxyScheme,
				UserName = extends.ProxyUsername == null ? null : Uri.EscapeDataString(extends.ProxyUsername)
			};

			return builder.Uri;
		}

		/// <summary>
		/// Builds an address string from the configured URI information.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static string GetAddress(this IProxyProperties extends)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			return extends.GetUri().ToString();
		}

		/// <summary>
		/// Sets URI information from the given address.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="address"></param>
		public static void SetUriFromAddress(this IProxyProperties extends, string address)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			Uri uri = new Uri(address, UriKind.RelativeOrAbsolute);

			extends.ProxyHost = uri.Host;
			extends.ProxyPassword = uri.GetPassword();
			extends.ProxyPort = (ushort)uri.Port;
			extends.ProxyUsername = uri.GetUserName();
			extends.ProxyScheme = uri.Scheme;
		}

		/// <summary>
		/// Updates the URI Properties instance where values are not already configured.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="address"></param>
		/// <param name="authMethod"></param>
		public static void ApplyDefaultValuesFromAddress(this IProxyProperties extends, string address,
		                                                 eProxyAuthenticationMethod? authMethod)
		{
			Uri uri = new Uri(address, UriKind.RelativeOrAbsolute);

			extends.ApplyDefaultValues(uri.GetUserName(),
			                           uri.GetPassword(),
			                           uri.Host,
			                           (ushort)uri.Port,
			                           uri.Scheme,
			                           authMethod);
		}
	}
}
