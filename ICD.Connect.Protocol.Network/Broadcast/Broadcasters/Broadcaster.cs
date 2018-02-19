namespace ICD.Connect.Protocol.Network.Broadcast.Broadcasters
{
    public sealed class Broadcaster : AbstractBroadcaster
	{
		private object m_BroadcastData;

		protected override object BroadcastData { get { return m_BroadcastData; } }

		public override void SetBroadcastData(object data)
		{
			m_BroadcastData = data;
		}
	}
}
