using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Protocol.Network.Broadcast
{
	public sealed class BroadcastEventArgs : GenericEventArgs<BroadcastData>
	{
		public BroadcastEventArgs(BroadcastData data)
			: base(data)
		{
		}
	}
}
