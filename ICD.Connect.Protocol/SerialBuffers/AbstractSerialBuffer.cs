using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Protocol.SerialBuffers
{
	public abstract class AbstractSerialBuffer : ISerialBuffer
	{
		/// <summary>
		/// Raised when a complete message has been buffered.
		/// </summary>
		public event EventHandler<StringEventArgs> OnCompletedSerial;

		private readonly Queue<string> m_Queue;
		private readonly SafeCriticalSection m_QueueSection;
		private readonly IcdManualResetEvent m_ParsingEvent;
		private bool m_Parsing;

		/// <summary>
		/// Sets while clearing, to tell the parse method to bail out early
		/// </summary>
		private bool m_Clearing;

		private bool Parsing
		{
			get { return m_Parsing; }
			set
			{
				if (m_Parsing == value)
					return;

				m_Parsing = value;

				if (value)
					m_ParsingEvent.Reset();
				else
					m_ParsingEvent.Set();
			}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractSerialBuffer()
		{
			m_Queue = new Queue<string>();
			m_QueueSection = new SafeCriticalSection();
			m_ParsingEvent = new IcdManualResetEvent(true);
		}

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
		public virtual void Clear()
		{
			m_Clearing = true;

			m_ParsingEvent.WaitOne();

			try
			{
				m_QueueSection.Execute(() => m_Queue.Clear());
				ClearFinal();
			}
			finally
			{
				m_Clearing = false;
			}
		}

		/// <summary>
		/// Override to clear any current state.
		/// </summary>
		protected abstract void ClearFinal();

		#endregion

		#region Private Methods

		/// <summary>
		/// Works through the queued messages to chunk up complete messages.
		/// </summary>
		private void Parse()
		{
			m_QueueSection.Enter();
			try
			{
				if (Parsing)
					return;
				Parsing = true;
			}
			finally
			{
				m_QueueSection.Leave();
			}

			try
			{
				string data = null;
				while (!m_Clearing && m_QueueSection.Execute(() => m_Queue.Dequeue(out data)))
					foreach (string serial in Process(data))
						OnCompletedSerial.Raise(this, new StringEventArgs(serial));
			}
			finally
			{
				m_QueueSection.Execute(() => Parsing = false);
			}
		}

		/// <summary>
		/// Override to process the given item for chunking.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		protected abstract IEnumerable<string> Process(string data);

		#endregion
	}
}
