using System;
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
		/// Gets/sets the configurable proxy type.
		/// </summary>
		eProxyType? ProxyType { get; set; }

		/// <summary>
		/// Gets/sets the configurable proxy authentication method.
		/// </summary>
		eProxyAuthenticationMethod? ProxyAuthenticationMethod { get; set; }
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
			extends.ProxyType = other.ProxyType;
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
		/// <param name="proxyType"></param>
		/// <param name="authMethod"></param>
		public static void ApplyDefaultValues(this IProxyProperties extends, string username, string password, string host,
		                                      ushort? port, eProxyType? proxyType, eProxyAuthenticationMethod? authMethod)
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

			if (extends.ProxyType == null)
				extends.ProxyType = proxyType;

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
			                          other.ProxyType,
			                          other.ProxyAuthenticationMethod);

			return output;
		}
	}
}
