using ICD.Common.Utils;

namespace ICD.Connect.Protocol.Data
{
	public abstract class AbstractSerialData : ISerialData
	{
		/// <summary>
		/// Serialize this instance to a string.
		/// </summary>
		/// <returns></returns>
		public abstract string Serialize();

		/// <summary>
		/// Gets the string representation for this instance.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return string.Format("{0}({1})", GetType().Name, StringUtils.ToRepresentation(Serialize()));
		}
	}
}
