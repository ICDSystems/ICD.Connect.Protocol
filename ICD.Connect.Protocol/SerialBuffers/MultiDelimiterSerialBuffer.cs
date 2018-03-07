using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Protocol.SerialBuffers
{
	public sealed class MultiDelimiterSerialBuffer : ISerialBuffer
	{
		public event EventHandler<StringEventArgs> OnCompletedSerial;

		private readonly StringBuilder m_RxData;
		private readonly Queue<string> m_Queue;

		private readonly SafeCriticalSection m_QueueSection;
		private readonly SafeCriticalSection m_ParseSection;

		private readonly char[] m_Delimiters;

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="delimiters"></param>
		public MultiDelimiterSerialBuffer(params byte[] delimiters)
			: this(delimiters.Select(d => (char)d))
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="delimiters"></param>
		public MultiDelimiterSerialBuffer(params char[] delimiters)
			: this(delimiters.Select(d => d))
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="delimiters"></param>
		public MultiDelimiterSerialBuffer(IEnumerable<byte> delimiters)
			: this(delimiters.Select(d => (char)d))
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="delimiters"></param>
		public MultiDelimiterSerialBuffer(IEnumerable<char> delimiters)
		{
			m_RxData = new StringBuilder();
			m_Queue = new Queue<string>();

			m_QueueSection = new SafeCriticalSection();
			m_ParseSection = new SafeCriticalSection();

			m_Delimiters = delimiters.Distinct().ToArray();
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

		/// <summary>
		/// Searches the enqueued serial data for the delimiter character.
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
					while (true)
					{
						int index = data.IndexOfAny(m_Delimiters);

						if (index < 0)
						{
							m_RxData.Append(data);
							break;
						}

						m_RxData.Append(data.Substring(0, index));
						data = data.Substring(index + 1);

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
	}
}
