using ICD.Common.Utils.EventArguments;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;

namespace ICD.Connect.Protocol.Crosspoints.EventArguments
{
	public sealed class CrosspointStatusEventArgs : GenericEventArgs<eCrosspointStatus>
	{
		public CrosspointStatusEventArgs(eCrosspointStatus data)
			: base(data)
		{
		}
	}
}
