using ICD.Connect.Protocol.Sigs;

namespace ICD.Connect.Protocol.XSig
{
	public interface IXSig
	{
		/// <summary>
		/// Gets the raw signal data.
		/// </summary>
		byte[] Data { get; }

		/// <summary>
		/// Gets the signal index.
		/// </summary>
		ushort Index { get; }

	    SigInfo ToSigInfo();

	    SigInfo ToSigInfo(ushort smartObjectId);
	}

	public interface IXSig<T> : IXSig
	{
		/// <summary>
		/// Gets the analog signal value.
		/// </summary>
		T Value { get; }
	}
}