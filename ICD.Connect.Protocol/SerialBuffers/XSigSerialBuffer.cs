using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
            {
                return;
            }

            try
            {
                string data = null;
                while (m_QueueSection.Execute(() => m_Queue.Dequeue(out data)))
                {
                    byte[] bytes = StringUtils.ToBytes(data);
                    m_RxData.AddRange(bytes);

                    // If there are not enough bytes dequeued for a digital sig, pull more
                    if (m_RxData.Count < 2)
                    {
                        continue;
                    }
                    //Check for Digital Pattern
                    if (Xsig.IsValidDigitalSigHeader(m_RxData.ToArray()))
                    {
                        char[] charsInMessage = m_RxData.Take(2).Select(b => (char)b).ToArray();
                        OnCompletedSerial.Raise(this, new StringEventArgs(new string(charsInMessage)));
                        m_RxData.RemoveRange(0, 2);
                        continue;
                    }
                    //If there are not enough bytes dequeued for an analog or serial sig, pull more
                    if (m_RxData.Count < 4)
                    {
                        continue;
                    }
                    //Check for Analog Pattern
                    if (Xsig.IsValidAnalogSigHeader(m_RxData.ToArray()))
                    {
                        char[] charsInMessage = m_RxData.Take(4).Select(b => (char)b).ToArray();
                        OnCompletedSerial.Raise(this, new StringEventArgs(new string(charsInMessage)));
                        m_RxData.RemoveRange(0, 4);
                        continue;
                    }
                    //check for Serial Pattern
                    if (Xsig.IsValidSerialSigHeader(m_RxData.ToArray()))
                    {
                        int indexOfSerialTerminator;
                        for (indexOfSerialTerminator = 2; indexOfSerialTerminator < m_RxData.Count; indexOfSerialTerminator++)
                        {
                            if (!Xsig.IsValidSerialSigTerminator(m_RxData[indexOfSerialTerminator]))
                            {
                                continue;
                            }
                            char[] charsInMessage = m_RxData.Take(indexOfSerialTerminator + 1).Select(b => (char)b).ToArray();
                            OnCompletedSerial.Raise(this, new StringEventArgs(new string(charsInMessage)));
                            m_RxData.RemoveRange(0, indexOfSerialTerminator + 1);
                            break;
                        }
                    }
                    // No valid pattern found, drop first byte and try again
                    m_RxData.RemoveAt(0);
                }
            }
            finally
            {
                m_ParseSection.Leave();
            }
        }
    }


}
