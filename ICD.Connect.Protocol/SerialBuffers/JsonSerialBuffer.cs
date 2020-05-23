using System.Collections.Generic;
using System.Text;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Protocol.SerialBuffers
{
	public sealed class JsonSerialBuffer : AbstractSerialBuffer
	{
		private static readonly char[] s_Tokens = {'{', '}'};

		private readonly StringBuilder m_RxData;

		private int m_OpenCount;
		private int m_CloseCount;

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public JsonSerialBuffer()
		{
			m_RxData = new StringBuilder();
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Override to clear any current state.
		/// </summary>
		protected override void ClearFinal()
		{
			m_RxData.Clear();
		}

		/// <summary>
		/// Override to process the given item for chunking.
		/// </summary>
		/// <param name="data"></param>
		protected override IEnumerable<string> Process(string data)
		{
			int start = 0;
			int left = 0;

			while (left < data.Length)
			{
				int index = data.IndexOfAny(s_Tokens, left);
				left = index + 1;

				// Simple case - No tokens in the data
				if (index < 0)
				{
					// Drop data without tokens if we are not inside an open json object
					if (m_RxData.Length > 0)
						m_RxData.Append(data);
					break;
				}

				// Harder case - Handle found token
				char token = data[index];

				switch (token)
				{
					case '{':
						// Skip leading nonsense
						if (m_OpenCount == 0)
							start = index;
						m_OpenCount++;
						break;

					case '}':
						// Skip over leading '}'
						if (m_CloseCount >= m_OpenCount)
							start = left;
						else
							m_CloseCount++;
						break;
				}

				if (m_OpenCount == 0 || m_OpenCount != m_CloseCount)
					continue;

				// We found a complete message
				m_OpenCount = 0;
				m_CloseCount = 0;

				yield return
					m_RxData.Length == 0
						? data.Substring(start, left - start)
						: m_RxData.Append(data, start, left - start).Pop();
			}
		}

		#endregion
	}
}
