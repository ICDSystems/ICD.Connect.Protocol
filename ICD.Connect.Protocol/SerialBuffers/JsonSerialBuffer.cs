using System;
using System.Collections.Generic;
using System.Text;
using ICD.Common.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Protocol.SerialBuffers
{
	[PublicAPI]
	public sealed class JsonSerialBuffer : ISerialBuffer
	{
		public event EventHandler<StringEventArgs> OnCompletedSerial;

		private readonly Queue<string> m_Queue;
		private readonly SafeCriticalSection m_QueueSection;
		private readonly SafeCriticalSection m_ParseSection;

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
			m_ParseSection.Execute(() => m_RxData.Clear());
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
					foreach (char character in data)
					{
						switch (character)
						{
							case '{':
								m_OpenCount++;
								break;
							case '}':
								m_CloseCount++;
								break;
						}

						// Trim any leading nonsense
						if (m_OpenCount == 0)
							continue;

						m_RxData.Append(character);

						if (m_OpenCount != m_CloseCount)
							continue;

						m_OpenCount = 0;
						m_CloseCount = 0;

						string output = m_RxData.Pop();
						OnCompletedSerial.Raise(this, new StringEventArgs(output));
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
