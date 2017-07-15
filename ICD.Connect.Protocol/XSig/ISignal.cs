namespace ICD.Connect.Protocol.XSig
{
	public interface ISignal
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

	public interface ISignal<T> : ISignal
	{
		/// <summary>
		/// Gets the analog signal value.
		/// </summary>
		T Value { get; }
	}
}