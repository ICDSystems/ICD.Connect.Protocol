using System.Collections.Generic;

namespace ICD.Connect.Protocol.SerialBuffers
{
	public sealed class BoundedSerialBuffer : AbstractSerialBuffer
	{
		private readonly char m_StartChar;
		private readonly char m_EndChar;

		private string m_RxData;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="startChar"></param>
		/// <param name="endChar"></param>
		public BoundedSerialBuffer(char startChar, char endChar)
		{
			m_StartChar = startChar;
			m_EndChar = endChar;
			m_RxData = string.Empty;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="startByte"></param>
		/// <param name="endByte"></param>
		public BoundedSerialBuffer(byte startByte, byte endByte)
			: this((char)startByte, (char)endByte)
		{
		}

		/// <summary>
		/// Override to clear any current state.
		/// </summary>
		protected override void ClearFinal()
		{
			m_RxData = string.Empty;
		}

		/// <summary>
		/// Override to process the given item for chunking.
		/// </summary>
		/// <param name="data"></param>
		protected override IEnumerable<string> Process(string data)
		{
			m_RxData += data;

			while (true)
			{
				// Find the header
				int firstHeader = m_RxData.IndexOf(m_StartChar);
				if (firstHeader < 0)
				{
					m_RxData = string.Empty;
					break;
				}

				if (firstHeader > 0)
					m_RxData = m_RxData.Substring(firstHeader);

				// Find the footer
				int firstFooter = m_RxData.IndexOf(m_EndChar);
				if (firstFooter < 0)
					break;

				yield return m_RxData.Substring(0, firstFooter + 1);

				m_RxData = m_RxData.Substring(firstFooter + 1);
			}
		}
	}
}
