using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Protocol.Network.Broadcast
{
	public sealed class BroadcastEventArgs<T> : GenericEventArgs<BroadcastData<T>>
	{
		public BroadcastEventArgs(BroadcastData<T> data)
			: base(data)
		{
		}
	}
}
