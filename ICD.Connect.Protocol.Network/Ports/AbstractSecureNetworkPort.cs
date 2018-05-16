namespace ICD.Connect.Protocol.Network.Ports
{
	public abstract class AbstractSecureNetworkPort<TSettings> : AbstractNetworkPort<TSettings>, ISecureNetworkPort
		where TSettings : ISecureNetworkPortSettings, new()
	{
		/// <summary>
		/// Gets/sets the username for authentication with the remote server.
		/// </summary>
		public abstract string Username { get; set; }

		/// <summary>
		/// Gets/sets the password for authentication with the remote server.
		/// </summary>
		public abstract ushort Password { get; set; }
	}
}
