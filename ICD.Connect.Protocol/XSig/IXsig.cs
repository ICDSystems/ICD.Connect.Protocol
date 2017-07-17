namespace ICD.Connect.Protocol.XSig
{
	public interface IXsig
	{
		/// <summary>
		/// Gets the raw signal data.
		/// </summary>
		byte[] Data { get; }

		/// <summary>
		/// Gets the signal index.
		/// </summary>
		ushort Index { get; }
	}

	public interface IXsig<T> : IXsig
	{
		/// <summary>
		/// Gets the analog signal value.
		/// </summary>
		T Value { get; }
	}
}