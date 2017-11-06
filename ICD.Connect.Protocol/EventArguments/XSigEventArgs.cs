using ICD.Common.Utils.EventArguments;
using ICD.Connect.Protocol.XSig;

namespace ICD.Connect.Protocol.EventArguments
{
	public sealed class XSigEventArgs : GenericEventArgs<IXSig>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public XSigEventArgs(IXSig data)
			: base(data)
		{
		}
	}
}
