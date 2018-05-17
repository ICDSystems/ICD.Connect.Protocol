using System;

namespace ICD.Connect.Protocol.Network.Settings
{
	public interface ISecureNetworkProperties : INetworkProperties
	{
		/// <summary>
		/// Gets/sets the configurable network username.
		/// </summary>
		string NetworkUsername { get; set; }

		/// <summary>
		/// Gets/sets the configurable network password.
		/// </summary>
		string NetworkPassword { get; set; }
	}

	public static class SecureNetworkPropertiesExtensions
	{
		/// <summary>
		/// Copies the configured properties from the given Secure Network Properties instance.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="other"></param>
		public static void Copy(this ISecureNetworkProperties extends, ISecureNetworkProperties other)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (other == null)
				throw new ArgumentNullException("other");

			(extends as INetworkProperties).Copy(other);

			extends.NetworkUsername = other.NetworkUsername;
			extends.NetworkPassword = other.NetworkPassword;
		}

		/// <summary>
		/// Updates the Network Properties instance where values are not already configured.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="port"></param>
		/// <param name="username"></param>
		/// <param name="password"></param>
		/// <param name="address"></param>
		public static void ApplyDefaultValues(this ISecureNetworkProperties extends, string address, ushort? port, string username, string password)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			extends.ApplyDefaultValues(address, port);

			if (extends.NetworkUsername == null)
				extends.NetworkUsername = username;

			if (extends.NetworkPassword == null)
				extends.NetworkPassword = password;
		}
	}
}
