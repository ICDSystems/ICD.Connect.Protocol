namespace ICD.Connect.Protocol.Network.Ports
{
	public abstract class AbstractSecureNetworkPortSettings : AbstractNetworkPortSettings, ISecureNetworkPortSettings
	{
		/// <summary>
		/// Gets/sets the configurable network username.
		/// </summary>
		public abstract string NetworkUsername { get; set; }

		/// <summary>
		/// Gets/sets the configurable network password.
		/// </summary>
		public abstract string NetworkPassword { get; set; }
	}
}
