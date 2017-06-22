using ICD.Common.EventArguments;

namespace ICD.Connect.Protocol.Network.Broadcast
{
	public class BroadcastEventArgs<T> : GenericEventArgs<Broadcast<T>>
	{
		public BroadcastEventArgs(Broadcast<T> data) : base(data)
		{
		}
	}
}
