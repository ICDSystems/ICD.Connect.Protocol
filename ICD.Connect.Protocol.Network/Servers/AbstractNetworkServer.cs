using System;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.Network.Servers
{
	public abstract class AbstractNetworkServer : AbstractServer<HostInfo>, INetworkServer
	{
		private const string ACCEPT_ALL = "0.0.0.0";

		#region Properties

		/// <summary>
		/// IP Address to accept connection from.
		/// </summary>
		public string AddressToAcceptConnectionFrom { get; set; }

		/// <summary>
		/// Port for server to listen on.
		/// </summary>
		public ushort Port { get; set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractNetworkServer()
		{
<<<<<<< HEAD
=======
			m_Logger = new ServiceLoggingContext(this);
			m_Clients = new IcdOrderedDictionary<uint, HostInfo>();
			m_ClientSendQueues = new Dictionary<uint, ThreadedWorkerQueue<string>>();
			m_ClientsSection = new SafeCriticalSection();

			Name = GetType().GetNameWithoutGenericArity();
>>>>>>> origin/Krang_v1.8
			AddressToAcceptConnectionFrom = ACCEPT_ALL;
		}

		#region Methods

		/// <summary>
		/// Override to add additional properties to the ToString representation.
		/// </summary>
		/// <param name="addPropertyAndValue"></param>
		protected override void BuildStringRepresentationProperties(Action<string, object> addPropertyAndValue)
		{
			base.BuildStringRepresentationProperties(addPropertyAndValue);

			addPropertyAndValue("Port", Port);
		}

		#endregion

		#region Console

		/// <summary>
<<<<<<< HEAD
=======
		/// Gets the name of the node.
		/// </summary>
		public virtual string ConsoleName { get { return string.IsNullOrEmpty(Name) ? GetType().GetNameWithoutGenericArity() : Name; } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public string ConsoleHelp { get { return null; } }

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public virtual IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield break;
		}

		/// <summary>
>>>>>>> origin/Krang_v1.8
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Port", Port);
		}

		#endregion
	}
}