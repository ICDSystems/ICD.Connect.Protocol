using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Network.Servers;

namespace ICD.Connect.Protocol.Network.Ports.Tcp
{
	public sealed partial class IcdTcpServer : AbstractNetworkServer
	{
		/// <summary>
		/// Called when a client is removed from the collection.
		/// </summary>
		/// <param name="clientId"></param>
		/// <param name="reason"></param>
		protected override void HandleClientRemoved(uint clientId, SocketStateEventArgs.eSocketStatus reason)
		{
			base.HandleClientRemoved(clientId, reason);

			UpdateListeningState();
		}
	}
}
