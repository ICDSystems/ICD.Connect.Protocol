using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Protocol.SerialBuffers
{
	public sealed class XmlSerialBuffer : ISerialBuffer
	{
		/// <summary>
		/// Raised when a complete message has been buffered.
		/// </summary>
		public event EventHandler<StringEventArgs> OnCompletedSerial;

		private readonly Queue<string> m_Queue;

		private readonly SafeCriticalSection m_QueueSection;
		private readonly SafeCriticalSection m_ParseSection;

		private string m_RxData;

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public XmlSerialBuffer()
		{
			m_RxData = string.Empty;
			m_Queue = new Queue<string>();

			m_QueueSection = new SafeCriticalSection();
			m_ParseSection = new SafeCriticalSection();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Enqueues the serial data.
		/// </summary>
		/// <param name="data"></param>
		public void Enqueue(string data)
		{
			m_QueueSection.Execute(() => m_Queue.Enqueue(data));
			Parse();
		}

		/// <summary>
		/// Clears all queued data in the buffer.
		/// </summary>
		public void Clear()
		{
			m_ParseSection.Execute(() => m_RxData = string.Empty);
			m_QueueSection.Execute(() => m_Queue.Clear());
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Searches the enqueued xml serial data for start and end elements.
		/// Complete strings are raised via the OnCompletedString event.
		/// </summary>
		private void Parse()
		{
			if (!m_ParseSection.TryEnter())
				return;

			try
			{
				string data = null;

				while (m_QueueSection.Execute(() => m_Queue.Dequeue(out data)))
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

						OnCompletedSerial.Raise(this, new StringEventArgs(complete));
					}
				}
			}
			finally
			{
				m_ParseSection.Leave();
			}
		}

		#endregion
	}
}
