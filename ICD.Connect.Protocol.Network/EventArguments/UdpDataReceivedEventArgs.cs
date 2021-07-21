using System;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.Network.EventArguments
{
	public sealed class UdpDataReceivedEventArgs : EventArgs
	{
		private readonly HostInfo m_Host;
		private readonly string m_Data;

		public HostInfo Host { get { return m_Host; } }
		public string Data { get { return m_Data; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="host"></param>
		/// <param name="data"></param>
		public UdpDataReceivedEventArgs(HostInfo host, string data)
		{
			m_Host = host;
			m_Data = data;
		}
	}
}