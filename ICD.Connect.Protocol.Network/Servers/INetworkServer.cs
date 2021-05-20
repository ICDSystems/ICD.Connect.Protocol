using ICD.Common.Properties;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.Network.Servers
{
	public interface INetworkServer : IServer<HostInfo>
	{
		/// <summary>
		/// IP Address to accept connection from.
		/// </summary>
		[PublicAPI]
		string AddressToAcceptConnectionFrom { get; set; }

		/// <summary>
		/// Port for server to listen on.
		/// </summary>
		[PublicAPI]
		ushort Port { get; set; }
	}
}
