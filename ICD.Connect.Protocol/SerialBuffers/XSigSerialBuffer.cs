using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Connect.Protocol.XSig;

namespace ICD.Connect.Protocol.SerialBuffers
{
	public sealed class XSigSerialBuffer : AbstractSerialBuffer
	{
		private readonly List<byte> m_RxData;

		/// <summary>
		/// Constructor.
		/// </summary>
		public XSigSerialBuffer()
		{
			m_RxData = new List<byte>();
		}

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
		/// <returns></returns>
		protected override IEnumerable<string> Process(string data)
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

				yield return StringUtils.ToString(m_RxData);
				m_RxData.Clear();
			}
		}
	}
}
