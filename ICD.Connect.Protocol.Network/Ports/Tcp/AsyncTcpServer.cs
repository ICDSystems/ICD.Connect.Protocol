using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Utils;

namespace ICD.Connect.Protocol.Network.Ports.Tcp
{
	public sealed partial class AsyncTcpServer : IConsoleNode, IDisposable
	{
		private const string ACCEPT_ALL = "0.0.0.0";
		private const int DEFAULT_MAX_NUMBER_OF_CLIENTS = 1;

		[PublicAPI] public const int MAX_NUMBER_OF_CLIENTS_SUPPORTED = 64;

		/// <summary>
		/// Raised when data is received from a client.
		/// </summary>
		public event EventHandler<TcpReceiveEventArgs> OnDataReceived;

		/// <summary>
		/// Raised when a client socket state changes.
		/// </summary>
		public event EventHandler<SocketStateEventArgs> OnSocketStateChange;

		private readonly Dictionary<uint, string> m_Connections;
		private readonly SafeCriticalSection m_ConnectionLock;

		private int m_MaxNumberOfClients;

		#region Properties

		/// <summary>
		/// Logging service for all your logging needs
		/// </summary>
		private ILoggerService Logger { get { return ServiceProvider.TryGetService<ILoggerService>(); } }

		/// <summary>
		/// IP Address to accept connection from.
		/// </summary>
		[PublicAPI]
		public string AddressToAcceptConnectionFrom { get; set; }

		/// <summary>
		/// Port for TCP Server to listen on.
		/// </summary>
		[PublicAPI]
		public ushort Port { get; set; }

		/// <summary>
		/// Returns true if the TCP Server actively listening for connections.
		/// </summary>
		[PublicAPI]
		public bool Active { get; private set; }

		/// <summary>
		/// Max number of connections supported by the TcpServer.
		/// </summary>
		[PublicAPI]
		public int MaxNumberOfClients
		{
			get { return m_MaxNumberOfClients; }
			set
			{
				if (value < 0 || value > MAX_NUMBER_OF_CLIENTS_SUPPORTED)
				{
					Logger.AddEntry(eSeverity.Warning,
					                "{0} - {1} is invalid for max number of clients. Clamping between 0 and {2}",
					                this, value, MAX_NUMBER_OF_CLIENTS_SUPPORTED);
				}

				m_MaxNumberOfClients = MathUtils.Clamp(value, 0, MAX_NUMBER_OF_CLIENTS_SUPPORTED);
			}
		}

		/// <summary>
		/// Assigns a name to the server for use with logging.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// When enabled prints the received data to the console.
		/// </summary>
		[PublicAPI]
		public eDebugMode DebugRx { get; set; }

		/// <summary>
		/// When enabled prints the transmitted data to the console.
		/// </summary>
		[PublicAPI]
		public eDebugMode DebugTx { get; set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Default constructor.
		/// </summary>
		[PublicAPI]
		public AsyncTcpServer()
			: this(0)
		{
		}

		/// <summary>
		/// Initializes the AsyncTcpServer
		/// </summary>
		/// <param name="port"></param>
		[PublicAPI]
		public AsyncTcpServer(ushort port)
			: this(port, DEFAULT_MAX_NUMBER_OF_CLIENTS)
		{
		}

		/// <summary>
		/// Initializes the AsyncTcpServer
		/// </summary>
		/// <param name="port">Port number to listen on</param>
		/// <param name="maxNumberOfClients">Max number of connected clients to support</param>
		[PublicAPI]
		public AsyncTcpServer(ushort port, int maxNumberOfClients)
		{
			m_Connections = new Dictionary<uint, string>();
			m_ConnectionLock = new SafeCriticalSection();

			AddressToAcceptConnectionFrom = ACCEPT_ALL;
			Port = port;
			MaxNumberOfClients = maxNumberOfClients;

			IcdEnvironment.OnEthernetEvent += IcdEnvironmentOnEthernetEvent;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			OnDataReceived = null;
			OnSocketStateChange = null;

			IcdEnvironment.OnEthernetEvent -= IcdEnvironmentOnEthernetEvent;

			Stop();
		}

		/// <summary>
		/// Stops and starts the server.
		/// </summary>
		public void Restart()
		{
			Stop();
			Start();
		}

		/// <summary>
		/// Gets the active client ids.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public IEnumerable<uint> GetClients()
		{
			return m_ConnectionLock.Execute(() => m_Connections.Keys.Order().ToArray());
		}

		/// <summary>
		/// Gets the string representation.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			ReprBuilder builder = new ReprBuilder(this);

			if (!string.IsNullOrEmpty(Name))
				builder.AppendProperty("Name", Name);

			if (Port != 0)
				builder.AppendProperty("Port", Port);

			return builder.ToString();
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Formats and prints the received data to the console.
		/// </summary>
		/// <param name="clientId"></param>
		/// <param name="data"></param>
		private void PrintRx(uint clientId, string data)
		{
			string context = string.Format("ClientId:{0}", clientId);
			DebugUtils.PrintRx(this, DebugRx, context, data);
		}

		/// <summary>
		/// Formats and prints the transmitted data to the console.
		/// </summary>
		/// <param name="clientId"></param>
		/// <param name="data"></param>
		private void PrintTx(uint clientId, string data)
		{
			string context = string.Format("ClientId:{0}", clientId);
			DebugUtils.PrintTx(this, DebugTx, context, data);
		}

		/// <summary>
		/// Handles an ethernet event
		/// </summary>
		/// <param name="adapter"></param>
		/// <param name="type"></param>
		private void IcdEnvironmentOnEthernetEvent(IcdEnvironment.eEthernetAdapterType adapter,
		                                           IcdEnvironment.eEthernetEventType type)
		{
			if (m_TcpListener == null)
				return;
#if SIMPLSHARP
			IcdEnvironment.eEthernetAdapterType adapterType =
				IcdEnvironment.GetEthernetAdapterType(m_TcpListener.EthernetAdapterToBindTo);
			if (adapter != adapterType && adapterType != IcdEnvironment.eEthernetAdapterType.EthernetUnknownAdapter)
				return;

#endif
			switch (type)
			{
				case IcdEnvironment.eEthernetEventType.LinkUp:
					if (Active)
						Start();
					break;

				case IcdEnvironment.eEthernetEventType.LinkDown:
					Stop();
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Called when a client connects.
		/// </summary>
		/// <param name="clientId"></param>
		private void AddClient(uint clientId)
		{
			RemoveClient(clientId);

			m_ConnectionLock.Enter();

			try
			{
				m_Connections[clientId] = GetHostnameForClientId(clientId);
				Logger.AddEntry(eSeverity.Notice, "{0} - Client {1} ({2}) connected", this, clientId, m_Connections[clientId]);
			}
			finally
			{
				m_ConnectionLock.Leave();
			}
		}

		/// <summary>
		/// Called when a client disconnects.
		/// </summary>
		/// <param name="clientId"></param>
		private void RemoveClient(uint clientId)
		{
			m_ConnectionLock.Enter();

			try
			{
				if (!m_Connections.ContainsKey(clientId))
					return;

				Logger.AddEntry(eSeverity.Notice, "{0} - Client {1} ({2}) disconnected", this, clientId, m_Connections[clientId]);
				m_Connections.Remove(clientId);
			}
			finally
			{
				m_ConnectionLock.Leave();
			}
		}

		/// <summary>
		/// Returns true if we are already tracking the given client.
		/// </summary>
		/// <param name="clientId"></param>
		/// <returns></returns>
		private bool ContainsClient(uint clientId)
		{
			return m_ConnectionLock.Execute(() => m_Connections.ContainsKey(clientId));
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public string ConsoleName { get { return "TcpServer"; } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public string ConsoleHelp { get { return null; } }

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield break;
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("Name", Name);
			addRow("Port", Port);
			addRow("Max Clients", MaxNumberOfClients);
			addRow("Active", Active);
			addRow("Debug Rx", DebugRx);
			addRow("Debug Tx", DebugTx);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new ConsoleCommand("Stop", "Stops the server", () => Stop());
			yield return new ConsoleCommand("Start", "Starts the server", () => Start());

			yield return new ConsoleCommand("EnableDebug", "Sets debug mode for TX/RX to Ascii",
									() =>
									{
										SetTxDebugMode(eDebugMode.Ascii);
										SetRxDebugMode(eDebugMode.Ascii);
									});

			yield return new ConsoleCommand("DisableDebug", "Sets debug mode for TX/RX to Off",
											() =>
											{
												SetTxDebugMode(eDebugMode.Off);
												SetRxDebugMode(eDebugMode.Off);
											});

			yield return new EnumConsoleCommand<eDebugMode>("SetDebugMode",
															p =>
															{
																SetTxDebugMode(p);
																SetRxDebugMode(p);
															});


			yield return new EnumConsoleCommand<eDebugMode>("SetDebugModeTx", p => SetTxDebugMode(p));
			yield return new EnumConsoleCommand<eDebugMode>("SetDebugModeRx", p => SetRxDebugMode(p));
		}

		private void SetTxDebugMode(eDebugMode mode)
		{
			DebugTx = mode;
		}

		private void SetRxDebugMode(eDebugMode mode)
		{
			DebugRx = mode;
		}

		#endregion
	}
}
