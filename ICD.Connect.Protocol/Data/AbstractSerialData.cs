namespace ICD.Connect.Protocol.Data
{
	public abstract class AbstractSerialData : ISerialData
	{
		/// <summary>
		/// Serialize this instance to a string.
		/// </summary>
		/// <returns></returns>
		public abstract string Serialize();
	}
}
