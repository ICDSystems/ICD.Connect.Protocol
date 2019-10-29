using ICD.Common.Utils;

namespace ICD.Connect.Protocol.Data
{
	/// <summary>
	/// Simply wraps serialized data.
	/// </summary>
	public sealed class SerialData : AbstractSerialData
	{
		private readonly string m_Data;

		/// <summary>
		/// Gets the wrapped serial string.
		/// </summary>
		public string Data { get { return m_Data; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public SerialData(string data)
		{
			m_Data = data;
		}

		/// <summary>
		/// Serialize this instance to a string.
		/// </summary>
		/// <returns></returns>
		public override string Serialize()
		{
			return m_Data;
		}

		public override string ToString()
		{
			ReprBuilder builder = new ReprBuilder(this);
			builder.AppendProperty("Data", m_Data);
			return builder.ToString();
		}
	}
}
