using ICD.Common.Utils;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Network.Servers;

namespace ICD.Connect.Protocol.Network.Ports.Tcp
{
	public abstract class AbstractTcpServer : AbstractNetworkServer, ITcpServer
	{
		private const ushort DEFAULT_PORT = 23;
		public const int MAX_NUMBER_OF_CLIENTS_SUPPORTED = 64;
		private const int DEFAULT_MAX_NUMBER_OF_CLIENTS = MAX_NUMBER_OF_CLIENTS_SUPPORTED;

		private int m_MaxNumberOfClients;

		/// <summary>
		/// Max number of connections supported by the server.
		/// </summary>
		public int MaxNumberOfClients
		{
			get { return m_MaxNumberOfClients; }
			set { m_MaxNumberOfClients = MathUtils.Clamp(value, 0, MAX_NUMBER_OF_CLIENTS_SUPPORTED); }
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractTcpServer()
		{
			Port = DEFAULT_PORT;
			MaxNumberOfClients = DEFAULT_MAX_NUMBER_OF_CLIENTS;
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Max Number of Clients", MaxNumberOfClients);
		}
	}
}
