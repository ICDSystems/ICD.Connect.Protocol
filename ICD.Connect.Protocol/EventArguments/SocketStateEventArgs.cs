using System;

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
	}
}
