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
		ushort Password { get; set; }
	}
}
