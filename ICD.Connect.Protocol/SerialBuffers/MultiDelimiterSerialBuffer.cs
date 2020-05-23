using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Protocol.SerialBuffers
{
	public sealed class MultiDelimiterSerialBuffer : AbstractSerialBuffer
	{
		private readonly StringBuilder m_RxData;

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

			m_Delimiters = delimiters.Distinct().ToArray();
		}

		#endregion

		#region Methods

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
				if (!string.IsNullOrEmpty(output))
					yield return output;
			}
		}

		#endregion
	}
}
