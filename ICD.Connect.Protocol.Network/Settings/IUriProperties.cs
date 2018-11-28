using System;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Protocol.Network.Settings
{
	public interface IUriProperties
	{
		/// <summary>
		/// Gets/sets the configurable URI username.
		/// </summary>
		string UriUsername { get; set; }

		/// <summary>
		/// Gets/sets the configurable URI password.
		/// </summary>
		string UriPassword { get; set; }

		/// <summary>
		/// Gets/sets the configurable URI host.
		/// </summary>
		[IpAddressSettingsProperty]
		string UriHost { get; set; }

		/// <summary>
		/// Gets/sets the configurable URI port.
		/// </summary>
		ushort? UriPort { get; set; }

		/// <summary>
		/// Gets/sets the configurable URI scheme.
		/// </summary>
		string UriScheme { get; set; }

		/// <summary>
		/// Gets/sets the configurable URI path.
		/// </summary>
		string UriPath { get; set; }

		/// <summary>
		/// Gets/sets the configurable URI query.
		/// </summary>
		string UriQuery { get; set; }

		/// <summary>
		/// Gets/sets the configurable URI fragment.
		/// </summary>
		string UriFragment { get; set; }

		/// <summary>
		/// Clears the configured values.
		/// </summary>
		void Clear();
	}

	public static class UriPropertiesExtensions
	{
		/// <summary>
		/// Copies the configured properties from the given URI Properties instance.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="other"></param>
		public static void Copy(this IUriProperties extends, IUriProperties other)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (other == null)
				throw new ArgumentNullException("other");

			extends.UriUsername = other.UriUsername;
			extends.UriPassword = other.UriPassword;
			extends.UriHost = other.UriHost;
			extends.UriPort = other.UriPort;
			extends.UriScheme = other.UriScheme;
			extends.UriPath = other.UriPath;
			extends.UriQuery = other.UriQuery;
			extends.UriFragment = other.UriFragment;
		}

		/// <summary>
		/// Updates the URI Properties instance where values are not already configured.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="username"></param>
		/// <param name="host"></param>
		/// <param name="port"></param>
		/// <param name="password"></param>
		/// <param name="scheme"></param>
		/// <param name="path"></param>
		/// <param name="query"></param>
		/// <param name="fragment"></param>
		public static void ApplyDefaultValues(this IUriProperties extends, string username, string password, string host,
		                                      ushort? port, string scheme, string path, string query, string fragment)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (extends.UriUsername == null)
				extends.UriUsername = username;

			if (extends.UriPassword == null)
				extends.UriPassword = password;

			if (extends.UriHost == null)
				extends.UriHost = host;

			if (extends.UriPort == null)
				extends.UriPort = port;

			if (extends.UriScheme == null)
				extends.UriScheme = scheme;

			if (extends.UriPath == null)
				extends.UriPath = path;

			if (extends.UriQuery == null)
				extends.UriQuery = query;

			if (extends.UriFragment == null)
				extends.UriFragment = fragment;
		}

		/// <summary>
		/// Creates a new properties instance, applying this instance over the top of the other instance.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="other"></param>
		/// <returns></returns>
		public static IUriProperties Superimpose(this IUriProperties extends, IUriProperties other)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (other == null)
				throw new ArgumentNullException("other");

			UriProperties output = new UriProperties();

			output.Copy(other);
			output.ApplyDefaultValues(extends.UriUsername, extends.UriPassword, extends.UriHost, extends.UriPort,
			                          extends.UriScheme, extends.UriPassword, extends.UriQuery, extends.UriFragment);

			return output;
		}

		/// <summary>
		/// Builds a URI from the configured properties.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static Uri GetUri(this IUriProperties extends)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			IcdUriBuilder builder = new IcdUriBuilder
			{
				Fragment = extends.UriFragment,
				Host = extends.UriHost,
				Password = extends.UriPassword,
				Path = extends.UriPath,
				Port = extends.UriPort ?? 0,
				Query = extends.UriQuery,
				Scheme = extends.UriScheme,
				UserName = extends.UriUsername
			};

			return builder.Uri;
		}

		/// <summary>
		/// Builds an address string from the available URI information.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static string GetAddressFromUri(this IUriProperties extends)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			IcdUriBuilder builder = new IcdUriBuilder
			{
				Fragment = extends.UriFragment,
				Host = extends.UriHost,
				Password = extends.UriPassword,
				Path = extends.UriPath,
				Port = extends.UriPort ?? 0,
				Query = extends.UriQuery,
				Scheme = extends.UriScheme,
				UserName = extends.UriUsername
			};

			return builder.ToString();
		}

		/// <summary>
		/// Sets URI information from the given address.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="address"></param>
		public static void SetUriFromAddress(this IUriProperties extends, string address)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			Uri uri = new Uri(address);

			extends.UriFragment = uri.Fragment;
			extends.UriHost = uri.Host;
			extends.UriPassword = uri.GetPassword();
			extends.UriPath = uri.AbsolutePath;
			extends.UriPort = (ushort)uri.Port;
			extends.UriQuery = uri.Query;
			extends.UriScheme = uri.Scheme;
			extends.UriUsername = uri.GetUserName();
		}
	}
}
