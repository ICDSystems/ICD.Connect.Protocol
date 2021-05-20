using ICD.Connect.Protocol.Network.Servers;

namespace ICD.Connect.Protocol.Network.Ports.Tcp
{
	public interface ITcpServer : INetworkServer
	{
		/// <summary>
		/// Max number of connections supported by the server.
		/// </summary>
		int MaxNumberOfClients { get; set; }
	}
}