using System;

namespace ICD.Connect.Protocol.Network.Broadcast.Broadcasters
{
	public delegate void BroadcastCallback(object data);

	public interface IBroadcaster : IDisposable
	{
		event EventHandler OnBroadcasting;

		event EventHandler<BroadcastEventArgs> OnBroadcastReceived;

		/// <summary>
		/// Called to send the broadcast.
		/// </summary>
		BroadcastCallback SendBroadcastData { get; set; }

		void SetBroadcastData(object data);

		void Broadcast();

		void HandleIncomingBroadcast(BroadcastData broadcastData);
	}
}
