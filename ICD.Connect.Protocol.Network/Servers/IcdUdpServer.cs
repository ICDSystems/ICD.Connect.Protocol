using System;
using System.Collections.Generic;
using ICD.Common.Logging.LoggingContexts;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Network.EventArguments;
using ICD.Connect.Protocol.Network.Ports.Udp;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Utils;

namespace ICD.Connect.Protocol.Network.Servers
{
	public sealed class IcdUdpServer : IDisposable, IConsoleNode
	{
		/// <summary>
		/// Raised when data is received from a client.
		/// </summary>
		[PublicAPI]
		public event EventHandler<UdpDataReceivedEventArgs> OnDataReceived;

		/// <summary>
		/// Raised when the server starts/stops listening.
		/// </summary>
		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnIsListeningStateChanged;

		/// <summary>
		/// Pool of sockets so multiple servers can listen on the same port
		/// </summary>
		private static readonly IcdUdpSocketPool s_SocketPool;

		/// <summary>
		/// Listening state of the socket
		/// </summary>
		private bool m_IsListening;

		private readonly ushort m_ListenPort;

		[CanBeNull]
		private IcdUdpSocket m_Socket;

		private readonly SafeCriticalSection m_SocketSection;

		[NotNull]
		private readonly ILoggingContext m_Logger;

		[NotNull]
		private ILoggingContext Logger
		{
			get { return m_Logger; }
		}

		[PublicAPI]
		public ushort ListenPort { get { return m_ListenPort; } }

		[PublicAPI]
		public bool IsListening
		{
			get { return m_IsListening; }
			private set
			{
				if (m_IsListening == value)
					return;

				m_IsListening = value;

				OnIsListeningStateChanged.Raise(this, value);
			}
		}

		/// <summary>
		/// Returns true if this instance has been disposed.
		/// </summary>
		[PublicAPI]
		public bool IsDisposed { get; private set; }

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

		/// <summary>
		/// Default deug mode to use when enabling debug.
		/// </summary>
		[PublicAPI]
		public eDebugMode DefaultDebugMode { get; set; }

		#region Constructors

		/// <summary>
		/// Static Constructor
		/// </summary>
		static IcdUdpServer()
		{
			s_SocketPool = new IcdUdpSocketPool();
		}

		/// <summary>
		/// Constructor for UDP Server
		/// Will always start a socket listening at 0.0.0.0 on the given port
		/// </summary>
		/// <param name="listenPort">Local port to listen on</param>
		public IcdUdpServer(ushort listenPort)
		{
			m_Logger = new ServiceLoggingContext(this);
			m_SocketSection = new SafeCriticalSection();
			m_ListenPort = listenPort;
			DefaultDebugMode = eDebugMode.MixedAsciiHex;
		}

		#endregion

		#region Methods

		public void Dispose()
		{
			IsDisposed = true;
			OnDataReceived = null;
			OnIsListeningStateChanged = null;
			Stop();
		}

		/// <summary>
		/// Starts the UDP Server
		/// </summary>
		[PublicAPI]
		public void Start()
		{
			IcdUdpSocket socket;

			m_SocketSection.Enter();
			try
			{
				// Skip if already started
				if (m_Socket != null)
					return;

				socket = s_SocketPool.GetSocket(this, m_ListenPort);
				m_Socket = socket;
			}
			finally
			{
				m_SocketSection.Leave();
			}

			Subscribe(socket);
			UpdateIsListeningState();


		}

		/// <summary>
		/// Stops and disables the server
		/// </summary>
		[PublicAPI]
		public void Stop()
		{
			m_SocketSection.Enter();

			try
			{
				if (m_Socket == null)
					return;

				Logger.Log(eSeverity.Debug, "Stopping IcdUdpServer port:{0}", ListenPort);

				Unsubscribe(m_Socket);
				s_SocketPool.ReturnSocket(this, m_Socket);
				m_Socket = null;
			}
			finally
			{
				m_SocketSection.Leave();
			}

			UpdateIsListeningState();
		}

		/// <summary>
		/// Sends data to the destination address
		/// Uses the listen port as the remote port
		/// </summary>
		/// <param name="data">Data to send</param>
		/// <param name="destinationAddress">Destination to send to</param>
		/// <returns></returns>
		[PublicAPI]
		public bool Send(string data, string destinationAddress)
		{
			return Send(data, destinationAddress, ListenPort);
		}

		/// <summary>
		/// Sends data to the destination address
		/// </summary>
		/// <param name="data">Data to send</param>
		/// <param name="destinationAddress">Destination to send to</param>
		/// <param name="destinationPort">Port to send to</param>
		/// <returns></returns>
		[PublicAPI]
		public bool Send(string data, string destinationAddress, ushort destinationPort)
		{
			IcdUdpSocket socket = null;

			m_SocketSection.Execute(() => socket = m_Socket);

			if (socket == null)
			{
				Logger.Log(eSeverity.Error, "Failed to send data to {0}:{1} - Wrapped client is null",
						   destinationAddress, destinationPort);
				return false;
			}

			try
			{
				socket.SendToAddress(data, destinationAddress, destinationPort);
				PrintTx(new HostInfo(destinationAddress, destinationPort).ToString(), data);
				return true;
			}
			catch (Exception e)
			{
				Logger.Log(eSeverity.Error, "Failed to send data to {0}:{1} - {2}",
						   destinationAddress, destinationPort, e.Message);
			}
			finally
			{
				UpdateIsListeningState();
			}

			return false;

		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Updates the is listening state from the underlying socket
		/// </summary>
		private void UpdateIsListeningState()
		{
			IsListening = m_Socket != null && m_Socket.IsConnected;
		}

		/// <summary>
		/// Formats and prints the received data to the console.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="data"></param>
		private void PrintRx(string context, string data)
		{
			DebugUtils.PrintRx(this, DebugRx, context, () => data);
		}

		/// <summary>
		/// Formats and prints the transmitted data to the console.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="data"></param>
		private void PrintTx(string context, string data)
		{
			DebugUtils.PrintTx(this, DebugTx, context, () => data);
		}

		#endregion

		#region Socket Callbacks

		private void Subscribe(IcdUdpSocket socket)
		{
			if (socket == null)
				return;

			socket.OnIsConnectedStateChanged += SocketOnIsConnectedStateChanged;
			socket.OnDataReceived += SocketOnDataReceived;
		}

		private void Unsubscribe(IcdUdpSocket socket)
		{
			if (socket == null)
				return;

			socket.OnIsConnectedStateChanged -= SocketOnIsConnectedStateChanged;
			socket.OnDataReceived -= SocketOnDataReceived;
		}

		private void SocketOnIsConnectedStateChanged(object sender, BoolEventArgs args)
		{
			UpdateIsListeningState();
		}

		private void SocketOnDataReceived(object sender, UdpDataReceivedEventArgs args)
		{
			PrintRx(args.Host.ToString(), args.Data);
			
			OnDataReceived.Raise(this, args);
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public string ConsoleName { get { return "IcdUdpServer"; }}

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public string ConsoleHelp { get { return string.Empty; } }

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
			addRow("ListenPort", ListenPort);
			addRow("IsConnected", IsListening);
			addRow("Debug Rx", DebugRx);
			addRow("Debug Tx", DebugTx);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new ConsoleCommand("EnableDebug", "Sets debug mode for TX/RX to the default debug mode",
								() =>
								{
									SetTxDebugMode(DefaultDebugMode);
									SetRxDebugMode(DefaultDebugMode);
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