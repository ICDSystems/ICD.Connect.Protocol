using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.Network.Ports.Udp;

namespace ICD.Connect.Protocol.Network.Servers
{
	/// <summary>
	/// We can only host a UDP server per network port,
	/// so this collection helps individual IcdUdpClients share.
	/// </summary>
	internal sealed class IcdUdpSocketPool
	{
		private readonly Dictionary<ushort, IcdUdpSocket> m_Sockets;
		private readonly Dictionary<IcdUdpSocket, Heartbeat.Heartbeat> m_Heartbeats;
		private readonly Dictionary<IcdUdpSocket, IcdHashSet<IcdUdpServer>> m_SocketClients; 
		private readonly SafeCriticalSection m_SocketsSection;

		/// <summary>
		/// Constructor.
		/// </summary>
		public IcdUdpSocketPool()
		{
			m_Sockets = new Dictionary<ushort, IcdUdpSocket>();
			m_Heartbeats = new Dictionary<IcdUdpSocket, Heartbeat.Heartbeat>();
			m_SocketClients = new Dictionary<IcdUdpSocket, IcdHashSet<IcdUdpServer>>();
			m_SocketsSection = new SafeCriticalSection();
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

			m_SocketsSection.Enter();

			try
			{
				IcdUdpSocket socket =
					m_Sockets.GetOrAddNew(port, () =>
					{
						IcdUdpSocket output = new IcdUdpSocket(IcdUdpSocket.DEFAULT_ACCEPT_ADDRESS, port, port);
						Heartbeat.Heartbeat heartbeat = new Heartbeat.Heartbeat(output);
						heartbeat.StartMonitoring();
						m_Heartbeats[output] = heartbeat;
						return output;
					});
				m_SocketClients.GetOrAddNew(socket).Add(server);
				return socket;
			}
			finally
			{
				m_SocketsSection.Leave();
			}
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
