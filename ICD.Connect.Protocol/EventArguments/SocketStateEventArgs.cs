using System;
using Crestron.SimplSharp.CrestronSockets;

#if SIMPLSHARP

#endif

namespace ICD.Connect.Protocol.EventArguments
{
	public sealed class SocketStateEventArgs : EventArgs
	{
		public enum eSocketStatus
		{
			SocketStatusNoConnect,
			SocketStatusWaiting,
			SocketStatusConnected,
			SocketStatusConnectFailed,
			SocketStatusBrokenRemotely,
			SocketStatusBrokenLocally,
			SocketStatusDnsLookup,
			SocketStatusDnsFailed,
			SocketStatusDnsResolved,
			SocketStatusLinkLost,
			SocketStatusSocketNotExist,
		}

		private readonly eSocketStatus m_SocketState;
		private readonly uint m_ClientId;

		public eSocketStatus SocketState { get { return m_SocketState; } }

		public uint ClientId { get { return m_ClientId; } }

		public SocketStateEventArgs(eSocketStatus socketState, uint clientId)
		{
			m_SocketState = socketState;
			m_ClientId = clientId;
		}

#if SIMPLSHARP
		public static eSocketStatus GetSocketStatus(SocketStatus status)
		{
			switch (status)
			{
				case SocketStatus.SOCKET_STATUS_NO_CONNECT:
					return eSocketStatus.SocketStatusNoConnect;
				case SocketStatus.SOCKET_STATUS_WAITING:
					return eSocketStatus.SocketStatusWaiting;
				case SocketStatus.SOCKET_STATUS_CONNECTED:
					return eSocketStatus.SocketStatusConnected;
				case SocketStatus.SOCKET_STATUS_CONNECT_FAILED:
					return eSocketStatus.SocketStatusConnectFailed;
				case SocketStatus.SOCKET_STATUS_BROKEN_REMOTELY:
					return eSocketStatus.SocketStatusBrokenRemotely;
				case SocketStatus.SOCKET_STATUS_BROKEN_LOCALLY:
					return eSocketStatus.SocketStatusBrokenLocally;
				case SocketStatus.SOCKET_STATUS_DNS_LOOKUP:
					return eSocketStatus.SocketStatusDnsLookup;
				case SocketStatus.SOCKET_STATUS_DNS_FAILED:
					return eSocketStatus.SocketStatusDnsFailed;
				case SocketStatus.SOCKET_STATUS_DNS_RESOLVED:
					return eSocketStatus.SocketStatusDnsResolved;
				case SocketStatus.SOCKET_STATUS_LINK_LOST:
					return eSocketStatus.SocketStatusLinkLost;
				case SocketStatus.SOCKET_STATUS_SOCKET_NOT_EXIST:
					return eSocketStatus.SocketStatusSocketNotExist;
				default:
					throw new ArgumentOutOfRangeException("status");
			}
		}
#endif
	}
}
