using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.IoT.Ports
{
	public interface IMqttClientSettings : IPortSettings
	{
		/// <summary>
		/// Gets/sets the hostname.
		/// </summary>
		string Hostname { get; set; }

		/// <summary>
		/// Gets/sets the network port.
		/// </summary>
		ushort Port { get; set; }

		/// <summary>
		/// Gets/sets the client id.
		/// </summary>
		string ClientId { get; set; }

		/// <summary>
		/// Gets/sets the username.
		/// </summary>
		string Username { get; set; }

		/// <summary>
		/// Gets/sets the password.
		/// </summary>
		string Password { get; set; }

		/// <summary>
		/// Gets/sets the secure mode.
		/// </summary>
		bool Secure { get; set; }
	}
}
