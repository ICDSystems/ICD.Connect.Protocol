using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.XSig;

namespace ICD.Connect.Protocol.SerialBuffers
{
	public sealed class XSigSerialBuffer : ISerialBuffer
	{
		public event EventHandler<StringEventArgs> OnCompletedSerial;

		private readonly List<byte> m_RxData;
		private readonly Queue<string> m_Queue;
		private readonly SafeCriticalSection m_QueueSection;
		private readonly SafeCriticalSection m_ParseSection;

		public XSigSerialBuffer()
		{
			m_RxData = new List<byte>();
			m_Queue = new Queue<string>();
			m_QueueSection = new SafeCriticalSection();
			m_ParseSection = new SafeCriticalSection();
		}

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

		/// <summary>
		/// Attempts to parse the current queued XSig buffer into one of three message types.
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
					byte[] bytes = StringUtils.ToBytes(data);

					foreach (byte b in bytes)
					{
						m_RxData.Add(b);

						while (m_RxData.Count > 0)
						{
							if (DigitalXSig.IsDigitalIncomplete(m_RxData) ||
							    AnalogXSig.IsAnalogIncomplete(m_RxData) ||
							    SerialXSig.IsSerialIncomplete(m_RxData))
								break;

							m_RxData.RemoveAt(0);
						}

						if (!DigitalXSig.IsDigital(m_RxData) &&
							!AnalogXSig.IsAnalog(m_RxData) &&
							!SerialXSig.IsSerial(m_RxData))
							continue;

						string serial = StringUtils.ToString(m_RxData);
						OnCompletedSerial.Raise(this, new StringEventArgs(serial));
						m_RxData.Clear();
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
