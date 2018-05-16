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
	}
}
