using System;
using System.Collections.Generic;

namespace ICD.Connect.Protocol.SerialBuffers
{
	public sealed class XmlSerialBuffer : AbstractSerialBuffer
	{
		private string m_RxData;

		/// <summary>
		/// Constructor.
		/// </summary>
		public XmlSerialBuffer()
		{
			m_RxData = string.Empty;
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
		/// <returns></returns>
		protected override IEnumerable<string> Process(string data)
		{
			m_RxData = m_RxData + data;

			while (true)
			{
				// Trim leading nonsense
				int openA = m_RxData.IndexOf('<');
				m_RxData = openA == -1 ? string.Empty : m_RxData.Substring(openA);

				// First close bracket
				int closeA = m_RxData.IndexOf('>');
				int attribute = m_RxData.IndexOf(' ');
				if (attribute != -1)
					closeA = Math.Min(closeA, attribute);

				if (closeA == -1)
					break;

				// Find the end element
				string elementName = m_RxData.Substring(1, closeA - 1);
				string closeElement = string.Format("</{0}>", elementName);

				int index = m_RxData.IndexOf(closeElement, StringComparison.Ordinal);
				if (index == -1)
					break;

				// Complete data
				string complete = m_RxData.Substring(0, index + closeElement.Length);
				m_RxData = complete.Length == m_RxData.Length ? string.Empty : m_RxData.Substring(complete.Length);

				yield return complete;
			}
		}
	}
}
