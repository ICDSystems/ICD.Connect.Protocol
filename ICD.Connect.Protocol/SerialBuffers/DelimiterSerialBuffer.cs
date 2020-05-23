using System.Collections.Generic;
using System.Text;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Protocol.SerialBuffers
{
	/// <summary>
	/// Serial Buffer provides a space to store serial data until a delimiter is found.
	/// </summary>
	public sealed class DelimiterSerialBuffer : AbstractSerialBuffer
	{
		private readonly StringBuilder m_RxData;

		private readonly char m_Delimiter;
		private readonly bool m_PassEmptyResponse;

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="delimiter"></param>
		public DelimiterSerialBuffer(byte delimiter)
			: this((char)delimiter, false)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="delimiter"></param>
		/// <param name="passEmptyResponse"></param>
		public DelimiterSerialBuffer(byte delimiter, bool passEmptyResponse)
			: this((char)delimiter, passEmptyResponse)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public DelimiterSerialBuffer(char delimiter)
			: this(delimiter, false)
		{
		}
		/// <summary>
		/// Constructor.
		/// </summary>
		public DelimiterSerialBuffer(char delimiter, bool passEmptyResponse)
		{
			m_RxData = new StringBuilder();

			m_Delimiter = delimiter;
			m_PassEmptyResponse = passEmptyResponse;
		}

		#endregion

		#region Private Methods

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
				int index = data.IndexOf(m_Delimiter);

				if (index < 0)
				{
					m_RxData.Append(data);
					break;
				}

				m_RxData.Append(data.Substring(0, index));
				data = data.Substring(index + 1);

				string output = m_RxData.Pop();
				if (m_PassEmptyResponse || !string.IsNullOrEmpty(output))
					yield return output;
			}
		}

		#endregion
	}
}
