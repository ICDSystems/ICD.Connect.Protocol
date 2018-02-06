using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Protocol.EventArguments;

namespace ICD.Connect.Protocol.Network.Tcp
{
	public sealed partial class AsyncTcpServer : IDisposable
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
	}
}
