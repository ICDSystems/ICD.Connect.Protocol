﻿using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
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
	public sealed partial class IcdTcpServer : IConsoleNode, IDisposable
	{
		private const string ACCEPT_ALL = "0.0.0.0";
		private const ushort DEFAULT_PORT = 23;
		private const int DEFAULT_MAX_NUMBER_OF_CLIENTS = 1;
		private const int DEFAULT_BUFFER_SIZE = 16384;

		[PublicAPI] public const int MAX_NUMBER_OF_CLIENTS_SUPPORTED = 64;

		/// <summary>
		/// Raised when data is received from a client.
		/// </summary>
		public event EventHandler<TcpReceiveEventArgs> OnDataReceived;

		/// <summary>
		/// Raised when a client socket state changes.
		/// </summary>
		public event EventHandler<SocketStateEventArgs> OnSocketStateChange;

		/// <summary>
		/// Raised when the server starts/stops listening.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnListeningStateChanged; 

		private readonly IcdOrderedDictionary<uint, HostInfo> m_Clients;
		private readonly SafeCriticalSection m_ClientsSection;

		private ILoggerService m_CachedLogger;

		private int m_MaxNumberOfClients;
		private bool m_Listening;

		#region Properties

		/// <summary>
		/// Logging service for all your logging needs
		/// </summary>
		private ILoggerService Logger
		{
			get { return m_CachedLogger ?? (m_CachedLogger = ServiceProvider.TryGetService<ILoggerService>()); }
		}

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
		/// Get or set the receive buffer size.
		/// </summary>
		[PublicAPI]
		public int BufferSize { get; set; }

		/// <summary>
		/// Tracks the enabled state of the TCP Server between getting/losing network connection.
		/// </summary>
		[PublicAPI]
		public bool Enabled { get; private set; }

		/// <summary>
		/// Gets the listening state of the TCP Server.
		/// </summary>
		[PublicAPI]
		public bool Listening
		{
			get { return m_Listening; }
			private set
			{
				if (value == m_Listening)
					return;

				m_Listening = value;

				eSeverity severity = m_Listening ? eSeverity.Notice : eSeverity.Warning;
				Logger.AddEntry(severity, "{0} - Listening set to {1}", this, m_Listening);

				OnListeningStateChanged.Raise(this, new BoolEventArgs(m_Listening));
			}
		}

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
		public IcdTcpServer()
			: this(DEFAULT_PORT)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="port"></param>
		[PublicAPI]
		public IcdTcpServer(ushort port)
			: this(port, DEFAULT_MAX_NUMBER_OF_CLIENTS)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="port">Port number to listen on</param>
		/// <param name="maxNumberOfClients">Max number of connected clients to support</param>
		[PublicAPI]
		public IcdTcpServer(ushort port, int maxNumberOfClients)
		{
			m_Clients = new IcdOrderedDictionary<uint, HostInfo>();
			m_ClientsSection = new SafeCriticalSection();

			AddressToAcceptConnectionFrom = ACCEPT_ALL;
			Port = port;
			BufferSize = DEFAULT_BUFFER_SIZE;
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
			OnListeningStateChanged = null;

			IcdEnvironment.OnEthernetEvent -= IcdEnvironmentOnEthernetEvent;

			Stop();
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
		[PublicAPI]
		public IEnumerable<uint> GetClients()
		{
			return m_ClientsSection.Execute(() => m_Clients.Keys.ToArray(m_Clients.Count));
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
		/// <param name="client"></param>
		/// <param name="data"></param>
		private void PrintRx(HostInfo client, string data)
		{
			string context = string.Format("Client:{0}", client);
			DebugUtils.PrintRx(this, DebugRx, context, data);
		}

		/// <summary>
		/// Formats and prints the transmitted data to the console.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="data"></param>
		private void PrintTx(HostInfo client, string data)
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
#if SIMPLSHARP
			if (m_TcpListener != null)
			{
				IcdEnvironment.eEthernetAdapterType adapterType =
					IcdEnvironment.GetEthernetAdapterType(m_TcpListener.EthernetAdapterToBindTo);
				if (adapterType != IcdEnvironment.eEthernetAdapterType.EthernetUnknownAdapter && adapter != adapterType)
					return;
			}
#endif

			switch (type)
			{
				case IcdEnvironment.eEthernetEventType.LinkUp:
					if (Enabled && !Listening)
					{
						Logger.AddEntry(eSeverity.Notice, "{0} - Regained connection, restarting server", this);
						Restart();
					}
					break;

				case IcdEnvironment.eEthernetEventType.LinkDown:
					Logger.AddEntry(eSeverity.Warning, "{0} - Lost connection, temporarily stopping server", this);
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
		/// <param name="reason"></param>
		private void AddClient(uint clientId, SocketStateEventArgs.eSocketStatus reason)
		{
			m_ClientsSection.Enter();

			try
			{
				if (m_Clients.ContainsKey(clientId))
					return;

				HostInfo hostInfo = GetHostInfoForClientId(clientId);
				m_Clients.Add(clientId, hostInfo);

				Logger.AddEntry(eSeverity.Notice, "{0} - Client {1} ({2}) connected", this, clientId, hostInfo);
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
		private void RemoveClient(uint clientId, SocketStateEventArgs.eSocketStatus reason)
		{
			m_ClientsSection.Enter();

			try
			{
				HostInfo hostInfo;
				if (!m_Clients.TryGetValue(clientId, out hostInfo))
					return;

				m_Clients.Remove(clientId);

				Logger.AddEntry(eSeverity.Notice, "{0} - Client {1} ({2}) disconnected", this, clientId, hostInfo);
			}
			finally
			{
				m_ClientsSection.Leave();
			}

			RaiseSocketStateChange(new SocketStateEventArgs(reason, clientId));
		}

		/// <summary>
		/// Returns true if we are already tracking the given client.
		/// </summary>
		/// <param name="clientId"></param>
		/// <returns></returns>
		private bool ContainsClient(uint clientId)
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
				Logger.AddEntry(eSeverity.Error, e, "{0} - Exception in OnSocketStateChange callback - {1}", this, e.Message);
			}
		}

		/// <summary>
		/// Raises the OnDataReceived event and logs any handler exceptions.
		/// </summary>
		/// <param name="eventArgs"></param>
		private void RaiseOnDataReceived(TcpReceiveEventArgs eventArgs)
		{
			try
			{
				OnDataReceived.Raise(this, eventArgs);
			}
			catch (Exception e)
			{
				Logger.AddEntry(eSeverity.Error, e, "{0} - Exception in OnDataReceived callback - {1}", this, e.Message);
			}
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

			yield return new ConsoleCommand("PrintClients", "Prints a table of the active clients", () => ConsolePrintClients());
		}

		private string ConsolePrintClients()
		{
			TableBuilder builder = new TableBuilder("Client ID", "HostInfo");

			foreach (uint client in GetClients().Order())
			{
				HostInfo clientInfo = GetHostInfoForClientId(client);
				builder.AddRow(client, clientInfo);
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
