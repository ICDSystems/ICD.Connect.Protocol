using ICD.Common.Properties;
using ICD.Connect.Protocol.Ports.DigitalInput;

namespace ICD.Connect.Protocol.Mock.Ports.DigitalInput
{
	public sealed class MockDigitalInputPort : AbstractDigitalInputPort<MockDigitalInputPortSettings>
	{
		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return true;
		}

		/// <summary>
		/// Emulates a digital input signal.
		/// </summary>
		/// <param name="state"></param>
		[PublicAPI]
		public void SetState(bool state)
		{
			State = state;
		}
	}
}
