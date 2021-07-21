using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Protocol.Network.Ports.Udp
{
	/// <summary>
	/// We can only host a UDP server per network port,
	/// so this collection helps individual IcdUdpClients share.
	/// </summary>
	internal sealed class IcdUdpSocketPool
	{
		private readonly Dictionary<ushort, IcdUdpSocket> m_Sockets;
		private readonly Dictionary<IcdUdpSocket, IcdHashSet<IcdUdpClient>> m_SocketClients; 
		private readonly SafeCriticalSection m_SocketsSection;

		/// <summary>
		/// Constructor.
		/// </summary>
		public IcdUdpSocketPool()
		{
			m_Sockets = new Dictionary<ushort, IcdUdpSocket>();
			m_SocketClients = new Dictionary<IcdUdpSocket, IcdHashSet<IcdUdpClient>>();
			m_SocketsSection = new SafeCriticalSection();
		}

		/// <summary>
		/// Lazy-loads a socket for the given port.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="port"></param>
		/// <returns></returns>
		public IcdUdpSocket GetSocket([NotNull] IcdUdpClient client, ushort port)
		{
			if (client == null)
				throw new ArgumentNullException("client");

			m_SocketsSection.Enter();

			try
			{
				IcdUdpSocket socket =
					m_Sockets.GetOrAddNew(port, () =>
					{
						IcdUdpSocket output = new IcdUdpSocket(port);
						output.Connect();
						return output;
					});
				m_SocketClients.GetOrAddNew(socket).Add(client);
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
		/// <param name="client"></param>
		/// <param name="socket"></param>
		public void ReturnSocket([NotNull] IcdUdpClient client, [NotNull] IcdUdpSocket socket)
		{
			if (client == null)
				throw new ArgumentNullException("client");

			if (socket == null)
				throw new ArgumentNullException("socket");

			m_SocketsSection.Enter();

			try
			{
				IcdHashSet<IcdUdpClient> clients;
				bool dispose = m_SocketClients.TryGetValue(socket, out clients) &&
				               clients.Remove(client) &&
				               clients.Count == 0;
				if (!dispose)
					return;

				m_Sockets.Remove(socket.Port);
				socket.Dispose();
			}
			finally
			{
				m_SocketsSection.Leave();
			}
		}
	}
}
