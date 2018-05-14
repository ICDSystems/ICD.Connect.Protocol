using System;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Protocol.Network.Settings
{
	public interface IUriProperties : INetworkProperties
	{
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
	}

	public static class UriPropertiesExtensions
	{
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
				Host = extends.NetworkAddress,
				Password = extends.Password,
				Port = extends.NetworkPort,
				Query = extends.UriQuery,
				Scheme = extends.UriScheme,
				UserName = extends.Username
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
			extends.NetworkAddress = uri.Host;
			extends.Password = uri.GetPassword();
			extends.NetworkPort = (ushort)uri.Port;
			extends.UriQuery = uri.Query;
			extends.UriScheme = uri.Scheme;
			extends.Username = uri.GetUserName();
		}
	}
}
