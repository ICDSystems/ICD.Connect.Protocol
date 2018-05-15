using System;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Protocol.Network.Settings
{
	public interface INetworkProperties
	{
		/// <summary>
		/// Gets/sets the configurable network address.
		/// </summary>
		[IpAddressSettingsProperty]
		string NetworkAddress { get; set; }

		/// <summary>
		/// Gets/sets the configurable network port.
		/// </summary>
		ushort NetworkPort { get; set; }

		/// <summary>
		/// Gets/sets the configurable network username.
		/// </summary>
		string NetworkUsername { get; set; }

		/// <summary>
		/// Gets/sets the configurable network password.
		/// </summary>
		string NetworkPassword { get; set; }
	}

	public static class NetworkPropertiesExtensions
	{
		/// <summary>
		/// Copies the configured properties from the given Network Properties instance.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="other"></param>
		public static void Copy(this INetworkProperties extends, INetworkProperties other)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (other == null)
				throw new ArgumentNullException("other");

			extends.NetworkAddress = other.NetworkAddress;
			extends.NetworkPort = other.NetworkPort;
			extends.NetworkAddress = other.NetworkAddress;
			extends.NetworkPort = other.NetworkPort;
		}
	}
}
