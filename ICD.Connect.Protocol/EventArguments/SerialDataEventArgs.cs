using ICD.Common.EventArguments;
using ICD.Connect.Protocol.Data;

namespace ICD.Connect.Protocol.EventArguments
{
	public sealed class SerialDataEventArgs : GenericEventArgs<ISerialData>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public SerialDataEventArgs(ISerialData data)
			: base(data)
		{
		}
	}
}
