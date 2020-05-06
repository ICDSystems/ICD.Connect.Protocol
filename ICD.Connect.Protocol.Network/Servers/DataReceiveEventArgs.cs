using System;
using System.Collections.Generic;
using ICD.Common.Utils;

namespace ICD.Connect.Protocol.Network.Servers
{
	public sealed class DataReceiveEventArgs : EventArgs
	{
		private readonly uint m_ClientId;
		private readonly string m_Data;

		public uint ClientId { get { return m_ClientId; } }

		public string Data { get { return m_Data; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="clientId"></param>
		/// <param name="data"></param>
		/// <param name="length"></param>
		public DataReceiveEventArgs(uint clientId, IEnumerable<byte> data, int length)
		{
			if (data == null)
				throw new ArgumentNullException("data");

			m_ClientId = clientId;
			m_Data = StringUtils.ToString(data, length);
		}
	}
}
