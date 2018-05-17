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
		ushort? NetworkPort { get; set; }
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
		}

		/// <summary>
		/// Updates the Network Properties instance where values are not already configured.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="address"></param>
		/// <param name="port"></param>
		public static void ApplyDefaultValues(this INetworkProperties extends, string address, ushort? port)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (extends.NetworkAddress == null)
				extends.NetworkAddress = address;

			if (extends.NetworkPort == null)
				extends.NetworkPort = port;
		}
	}
}
