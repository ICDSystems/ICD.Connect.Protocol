using ICD.Connect.Protocol.Ports.RelayPort;

namespace ICD.Connect.Protocol.Mock.Ports.RelayPort
{
	public sealed class MockRelayPort : AbstractRelayPort<MockRelayPortSettings>
	{
		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return false;
		}

		/// <summary>
		/// Open the relay
		/// </summary>
		public override void Open()
		{
			Closed = false;
		}

		/// <summary>
		/// Close the relay
		/// </summary>
		public override void Close()
		{
			Closed = true;
		}
	}
}
