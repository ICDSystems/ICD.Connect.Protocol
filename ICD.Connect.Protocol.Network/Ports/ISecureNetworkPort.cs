using ICD.Connect.Protocol.Network.Settings;

namespace ICD.Connect.Protocol.Network.Ports
{
	public interface ISecureNetworkPort : INetworkPort
	{
		/// <summary>
		/// Gets/sets the username for authentication with the remote server.
		/// </summary>
		string Username { get; set; }

		/// <summary>
		/// Gets/sets the password for authentication with the remote server.
		/// </summary>
		string Password { get; set; }

		/// <summary>
		/// Applies the given device configuration properties to the port.
		/// </summary>
		/// <param name="properties"></param>
		void ApplyDeviceConfiguration(ISecureNetworkProperties properties);

		/// <summary>
		/// Applies the given configuration properties to the port.
		/// </summary>
		/// <param name="properties"></param>
		void ApplyConfiguration(ISecureNetworkProperties properties);
	}
}
