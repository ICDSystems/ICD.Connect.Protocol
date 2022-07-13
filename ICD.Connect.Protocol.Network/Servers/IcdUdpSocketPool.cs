using System;
using System.Collections.Generic;
using ICD.Common.Logging.LoggingContexts;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Protocol.Network.Ports.Udp;

namespace ICD.Connect.Protocol.Network.Servers
{
	/// <summary>
	/// We can only host a UDP server per network port,
	/// so this collection helps individual IcdUdpClients share.
	/// </summary>
	internal sealed class IcdUdpSocketPool
	{
        /// <summary>
        /// Holds sockets, key is port number
        /// </summary>
		private readonly Dictionary<ushort, IcdUdpSocket> m_Sockets;
        
        /// <summary>
        /// Holds socket accept addresses, key is port number
        /// </summary>
	    private readonly Dictionary<ushort, string> m_SocketAcceptAddresses;

        /// <summary>
        /// Holds heartbeats for sockets, key is socket
        /// </summary>
		private readonly Dictionary<IcdUdpSocket, Heartbeat.Heartbeat> m_Heartbeats;

        /// <summary>
        /// Holds a collection of clients for each socket
        /// </summary>
		private readonly Dictionary<IcdUdpSocket, IcdHashSet<IcdUdpServer>> m_SocketClients; 
		
        private readonly SafeCriticalSection m_SocketsSection;
	    
        private readonly ILoggingContext m_Logger;


	    private ILoggingContext Logger
	    {
	        get { return m_Logger; }
	    }

	    /// <summary>
		/// Constructor.
		/// </summary>
		public IcdUdpSocketPool()
		{
			m_Sockets = new Dictionary<ushort, IcdUdpSocket>();
            m_SocketAcceptAddresses = new Dictionary<ushort, string>();
			m_Heartbeats = new Dictionary<IcdUdpSocket, Heartbeat.Heartbeat>();
			m_SocketClients = new Dictionary<IcdUdpSocket, IcdHashSet<IcdUdpServer>>();
			m_SocketsSection = new SafeCriticalSection();
            m_Logger = new ServiceLoggingContext(this);
		}

		/// <summary>
		/// Lazy-loads a socket for the given local port.
		/// </summary>
		/// <param name="server"></param>
		/// <param name="port"></param>
		/// <returns></returns>
		public IcdUdpSocket GetSocket([NotNull] IcdUdpServer server, ushort port)
		{
			if (server == null)
				throw new ArgumentNullException("client");

		    return GetSocket(server, port, IcdUdpSocket.DEFAULT_ACCEPT_ADDRESS);
		}

	    public IcdUdpSocket GetSocket([NotNull] IcdUdpServer server, ushort port, [NotNull] string acceptAddress)
	    {
            if (server == null)
                throw new ArgumentNullException("client");
	        if (acceptAddress == null)
	            throw new ArgumentNullException("acceptAddress");

	        m_SocketsSection.Enter();

            try
            {
                // Check if accept address is differnet and throw a warning
                string existingAcceptAddress;
                if (m_SocketAcceptAddresses.TryGetValue(port, out existingAcceptAddress) &
                    !string.Equals(existingAcceptAddress, acceptAddress))
                    Logger.Log(eSeverity.Error,
                               "Tried to create UDP socket on port {0} with accept address {1} but accept address {2} already in use. Using existing listen address.",
                               port, acceptAddress, existingAcceptAddress);

                // Get or create a new socket
                IcdUdpSocket socket = m_Sockets.GetOrAddNew(port, ()=> CreateNewSocket(port, acceptAddress));
                m_SocketClients.GetOrAddNew(socket).Add(server);
                return socket;
            }
            finally
            {
                m_SocketsSection.Leave();
            }
	    }

	    /// <summary>
	    /// Creates a new UDP socket for the given port
	    /// Only use inside the context of other checks
	    /// </summary>
	    /// <param name="port"></param>
	    /// <param name="acceptAddress"></param>
	    /// <returns></returns>
	    private IcdUdpSocket CreateNewSocket(ushort port, string acceptAddress)
	    {
            IcdUdpSocket output = new IcdUdpSocket(acceptAddress, port, port);
            Heartbeat.Heartbeat heartbeat = new Heartbeat.Heartbeat(output);
            heartbeat.StartMonitoring();
            m_Heartbeats[output] = heartbeat;
            m_SocketAcceptAddresses[port] = acceptAddress;
            return output;
	    }

		/// <summary>
		/// Returns a socket once the client is done with it.
		/// </summary>
		/// <param name="server"></param>
		/// <param name="socket"></param>
		public void ReturnSocket([NotNull] IcdUdpServer server, [NotNull] IcdUdpSocket socket)
		{
			if (server == null)
				throw new ArgumentNullException("client");

			if (socket == null)
				throw new ArgumentNullException("socket");

			m_SocketsSection.Enter();

			try
			{
				IcdHashSet<IcdUdpServer> servers;
				bool dispose = m_SocketClients.TryGetValue(socket, out servers) &&
				               servers.Remove(server) &&
				               servers.Count == 0;
				if (!dispose)
					return;

				// Stop heartbeat monitoring and dispose
				Heartbeat.Heartbeat heartbeat;
				if (m_Heartbeats.TryGetValue(socket, out heartbeat))
				{
					heartbeat.StopMonitoring();
					heartbeat.Dispose();
					m_Heartbeats.Remove(socket);
				}

                // Remove accept address
                m_SocketAcceptAddresses.Remove(socket.LocalPort);

				// Remove socket and dispose
				m_Sockets.Remove(socket.LocalPort);
				socket.Dispose();
			}
			finally
			{
				m_SocketsSection.Leave();
			}
		}
	}
}
