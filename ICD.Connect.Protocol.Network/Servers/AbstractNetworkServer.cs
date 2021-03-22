using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Logging.LoggingContexts;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Utils;

namespace ICD.Connect.Protocol.Network.Servers
{
	public abstract class AbstractNetworkServer : INetworkServer
	{
		private const string ACCEPT_ALL = "0.0.0.0";
		private const ushort DEFAULT_PORT = 23;
		private const int DEFAULT_BUFFER_SIZE = 16384;
		public const int MAX_NUMBER_OF_CLIENTS_SUPPORTED = 64;
		private const int DEFAULT_MAX_NUMBER_OF_CLIENTS = MAX_NUMBER_OF_CLIENTS_SUPPORTED;

		private readonly ILoggingContext m_Logger;

		/// <summary>
		/// Raised when data is received from a client.
		/// </summary>
		public event EventHandler<DataReceiveEventArgs> OnDataReceived;

		/// <summary>
		/// Raised when a client socket state changes.
		/// </summary>
		public event EventHandler<SocketStateEventArgs> OnSocketStateChange;

		/// <summary>
		/// Raised when the server starts/stops listening.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnListeningStateChanged;

		private readonly IcdOrderedDictionary<uint, HostInfo> m_Clients;
		private readonly Dictionary<uint, ThreadedWorkerQueue<string>> m_ClientSendQueues;
		private readonly SafeCriticalSection m_ClientsSection;

		private bool m_Listening;
		private int m_MaxNumberOfClients;

		#region Properties

		/// <summary>
		/// IP Address to accept connection from.
		/// </summary>
		public string AddressToAcceptConnectionFrom { get; set; }

		/// <summary>
		/// Port for server to listen on.
		/// </summary>
		public ushort Port { get; set; }

		/// <summary>
		/// Get or set the receive buffer size.
		/// </summary>
		public int BufferSize { get; set; }

		/// <summary>
		/// Tracks the enabled state of the server between getting/losing network connection.
		/// </summary>
		public bool Enabled { get; protected set; }

		/// <summary>
		/// Gets the listening state of the server.
		/// </summary>
		public bool Listening
		{
			get { return m_Listening; }
			protected set
			{
				if (value == m_Listening)
					return;

				m_Listening = value;

				Logger.LogSetTo(m_Listening ? eSeverity.Notice : eSeverity.Warning, "Listening", m_Listening);

				OnListeningStateChanged.Raise(this, new BoolEventArgs(m_Listening));
			}
		}

		/// <summary>
		/// Max number of connections supported by the server.
		/// </summary>
		public int MaxNumberOfClients
		{
			get { return m_MaxNumberOfClients; }
			set { m_MaxNumberOfClients = MathUtils.Clamp(value, 0, MAX_NUMBER_OF_CLIENTS_SUPPORTED); }
		}

		/// <summary>
		/// Number of active connections.
		/// </summary>
		public int NumberOfClients { get { return m_ClientsSection.Execute(() => m_Clients.Count); } }

		/// <summary>
		/// Assigns a name to the server for use with logging.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// When enabled prints the received data to the console.
		/// </summary>
		public eDebugMode DebugRx { get; set; }

		/// <summary>
		/// When enabled prints the transmitted data to the console.
		/// </summary>
		public eDebugMode DebugTx { get; set; }

		/// <summary>
		/// Gets the logger.
		/// </summary>
		protected ILoggingContext Logger { get { return m_Logger; } }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractNetworkServer()
		{
			m_Logger = new ServiceLoggingContext(this);
			m_Clients = new IcdOrderedDictionary<uint, HostInfo>();
			m_ClientSendQueues = new Dictionary<uint, ThreadedWorkerQueue<string>>();
			m_ClientsSection = new SafeCriticalSection();

			AddressToAcceptConnectionFrom = ACCEPT_ALL;
			Port = DEFAULT_PORT;
			BufferSize = DEFAULT_BUFFER_SIZE;
			MaxNumberOfClients = DEFAULT_MAX_NUMBER_OF_CLIENTS;

			IcdEnvironment.OnEthernetEvent += IcdEnvironmentOnEthernetEvent;
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			OnDataReceived = null;
			OnSocketStateChange = null;
			OnListeningStateChanged = null;

			Stop();
		}

		#endregion

		#region Methods

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

		/// <summary>
		/// Stops and starts the server.
		/// </summary>
		public void Restart()
		{
			Stop(false);
			Start();
		}

		/// <summary>
		/// Gets the active client ids.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<uint> GetClients()
		{
			return m_ClientsSection.Execute(() => m_Clients.Keys.ToArray(m_Clients.Count));
		}

		/// <summary>
		/// Starts the TCP Server
		/// </summary>
		public abstract void Start();

		/// <summary>
		/// Stops and Disables the Server
		/// </summary>
		public void Stop()
		{
			Stop(true);
		}

		/// <summary>
		/// Stops the Server
		/// </summary>
		protected abstract void Stop(bool disable);

		/// <summary>
		/// Sends the data to all connected clients.
		/// </summary>
		/// <param name="data"></param>
		public void Send(string data)
		{
			uint[] clients = GetClients().ToArray();
			if (clients.Length == 0)
				return;

			foreach (uint clientId in clients)
				Send(clientId, data);
		}

		/// <summary>
		/// Sends a Byte for Byte string (ISO-8859-1)
		/// </summary>
		/// <param name="clientId">Client Identifier for Connection</param>
		/// <param name="data">String in ISO-8859-1 Format</param>
		/// <returns></returns>
		public void Send(uint clientId, string data)
		{
			m_ClientsSection.Enter();
			try
			{
				ThreadedWorkerQueue<string> queue;
				if (!m_ClientSendQueues.TryGetValue(clientId, out queue))
				{
					Logger.Log(eSeverity.Warning, "Unable to send data to unconnected client {0}", clientId);
					RemoveClient(clientId, SocketStateEventArgs.eSocketStatus.SocketStatusNoConnect);
					return;
				}
				queue.Enqueue(data);
			}
			finally
			{
				m_ClientsSection.Leave();
			}
		}

		/// <summary>
		/// Gets the address and port for the client with the given id.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="info"></param>
		/// <returns></returns>
		public bool TryGetClientInfo(uint client, out HostInfo info)
		{
			m_ClientsSection.Enter();

			try
			{
				return m_Clients.TryGetValue(client, out info);
			}
			finally
			{
				m_ClientsSection.Leave();
			}
		}

		/// <summary>
		/// Returns true if the client is connected.
		/// </summary>
		/// <param name="client"></param>
		/// <returns></returns>
		public bool ClientConnected(uint client)
		{
			return m_ClientsSection.Execute(() => m_Clients.ContainsKey(client));
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Called in a worker thread to send the data to the specified client
		/// This should send the data synchronously to ensure in-order transmission
		/// If this blocks, it will stop all data from being sent
		/// </summary>
		/// <param name="clientId"></param>
		/// <param name="data"></param>
		protected abstract void SendWorkerAction(uint clientId, string data);

		/// <summary>
		/// Formats and prints the received data to the console.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="data"></param>
		protected void PrintRx(HostInfo client, string data)
		{
			string context = string.Format("Client:{0}", client);
			DebugUtils.PrintRx(this, DebugRx, context, data);
		}

		/// <summary>
		/// Formats and prints the transmitted data to the console.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="data"></param>
		protected void PrintTx(HostInfo client, string data)
		{
			string context = string.Format("Client:{0}", client);
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
			switch (type)
			{
				case IcdEnvironment.eEthernetEventType.LinkUp:
					if (Enabled && !Listening)
					{
						Logger.Log(eSeverity.Notice, "Regained connection, restarting server");
						Restart();
					}
					break;

				case IcdEnvironment.eEthernetEventType.LinkDown:
					Logger.Log(eSeverity.Warning, "Lost connection, temporarily stopping server");
					Stop(false);
					break;

				default:
					throw new ArgumentOutOfRangeException("type");
			}
		}

		/// <summary>
		/// Called when a client connects.
		/// </summary>
		/// <param name="clientId"></param>
		/// <param name="hostInfo"></param>
		/// <param name="reason"></param>
		protected void AddClient(uint clientId, HostInfo hostInfo, SocketStateEventArgs.eSocketStatus reason)
		{
			m_ClientsSection.Enter();

			try
			{
				if (m_Clients.ContainsKey(clientId))
					return;

				m_Clients.Add(clientId, hostInfo);
				m_ClientSendQueues.Add(clientId, new ThreadedWorkerQueue<string>(data => SendWorkerAction(clientId, data)));

				Logger.Log(eSeverity.Notice, "Client {0} ({1}) connected", clientId, hostInfo);
			}
			finally
			{
				m_ClientsSection.Leave();
			}

			RaiseSocketStateChange(new SocketStateEventArgs(reason, clientId));
		}

		/// <summary>
		/// Called when a client disconnects.
		/// </summary>
		/// <param name="clientId"></param>
		/// <param name="reason"></param>
		protected void RemoveClient(uint clientId, SocketStateEventArgs.eSocketStatus reason)
		{
			m_ClientsSection.Enter();

			try
			{
				HostInfo hostInfo;
				if (!m_Clients.TryGetValue(clientId, out hostInfo))
					return;

				m_Clients.Remove(clientId);

				ThreadedWorkerQueue<string> clientQueue;
				if (m_ClientSendQueues.TryGetValue(clientId, out clientQueue))
				{
					m_ClientSendQueues.Remove(clientId);
					clientQueue.Clear();
				}
				

				Logger.Log(eSeverity.Notice, "Client {0} ({1}) disconnected", clientId, hostInfo);
			}
			finally
			{
				m_ClientsSection.Leave();
			}

			HandleClientRemoved(clientId, reason);

			RaiseSocketStateChange(new SocketStateEventArgs(reason, clientId));
		}

		/// <summary>
		/// Called when a client is removed from the collection.
		/// </summary>
		/// <param name="clientId"></param>
		/// <param name="reason"></param>
		protected virtual void HandleClientRemoved(uint clientId, SocketStateEventArgs.eSocketStatus reason)
		{
		}

		/// <summary>
		/// Returns true if we are already tracking the given client.
		/// </summary>
		/// <param name="clientId"></param>
		/// <returns></returns>
		protected bool ContainsClient(uint clientId)
		{
			return m_ClientsSection.Execute(() => m_Clients.ContainsKey(clientId));
		}

		/// <summary>
		/// Raises the OnSocketStateChange event and logs any handler exceptions.
		/// </summary>
		/// <param name="eventArgs"></param>
		private void RaiseSocketStateChange(SocketStateEventArgs eventArgs)
		{
			try
			{
				OnSocketStateChange.Raise(this, eventArgs);
			}
			catch (Exception e)
			{
				Logger.Log(eSeverity.Error, e, "Exception in OnSocketStateChange callback - {0}", e.Message);
			}
		}

		/// <summary>
		/// Raises the OnDataReceived event and logs any handler exceptions.
		/// </summary>
		/// <param name="eventArgs"></param>
		protected void RaiseOnDataReceived(DataReceiveEventArgs eventArgs)
		{
			try
			{
				OnDataReceived.Raise(this, eventArgs);
			}
			catch (Exception e)
			{
				Logger.Log(eSeverity.Error, e, "Exception in OnDataReceived callback - {0}", e.Message);
			}
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public string ConsoleName { get { return Name; } }

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
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public virtual void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("Name", Name);
			addRow("Port", Port);
			addRow("Active Clients", string.Format("{0}/{1}", NumberOfClients, MaxNumberOfClients));
			addRow("Listening", Listening);
			addRow("Enabled", Enabled);
			addRow("Debug Rx", DebugRx);
			addRow("Debug Tx", DebugTx);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public virtual IEnumerable<IConsoleCommand> GetConsoleCommands()
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

			yield return new GenericConsoleCommand<uint, string>("Send", "Send<clientId, data>", (clientId, data) => Send(clientId, data));

			yield return new ConsoleCommand("PrintClients", "Prints a table of the active clients", () => ConsolePrintClients());
		}

		private string ConsolePrintClients()
		{
			TableBuilder builder = new TableBuilder("Client ID", "HostInfo");

			foreach (uint client in GetClients().Order())
			{
				HostInfo info;
				TryGetClientInfo(client, out info);

				builder.AddRow(client, info);
			}

			return builder.ToString();
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