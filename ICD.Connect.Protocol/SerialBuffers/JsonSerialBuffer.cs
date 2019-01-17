using System;
using System.Collections.Generic;
using System.Text;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Protocol.SerialBuffers
{
	[PublicAPI]
	public sealed class JsonSerialBuffer : ISerialBuffer
	{
		/// <summary>
		/// Raised when a complete message has been buffered.
		/// </summary>
		public event EventHandler<StringEventArgs> OnCompletedSerial;

		private readonly Queue<string> m_Queue;
		private readonly SafeCriticalSection m_QueueSection;
		private readonly SafeCriticalSection m_ParseSection;

		private readonly StringBuilder m_RxData;

		private static readonly char[] s_Tokens = {'{', '}'};

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
			if (data == null)
				throw new ArgumentNullException("data");

			m_QueueSection.Execute(() => m_Queue.Enqueue(data));
			Parse();
		}

		/// <summary>
		/// Clears all queued data in the buffer.
		/// </summary>
		public void Clear()
		{
			m_ParseSection.Enter();
			m_QueueSection.Enter();

			try
			{
				m_RxData.Clear();
				m_Queue.Clear();
			}
			finally
			{
				m_ParseSection.Leave();
				m_QueueSection.Leave();
			}
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

							data = string.Empty;
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

						string output =
							m_RxData.Length == 0
								? data.Substring(start, left - start)
								: m_RxData.Append(data, start, left - start).Pop();

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
