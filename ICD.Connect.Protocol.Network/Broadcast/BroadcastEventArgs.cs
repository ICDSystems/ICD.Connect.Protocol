using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Protocol.Network.Broadcast
{
	public sealed class BroadcastEventArgs<T> : GenericEventArgs<Broadcast<T>>
	{
		public BroadcastEventArgs(Broadcast<T> data) : base(data)
		{
		}
	}
}
