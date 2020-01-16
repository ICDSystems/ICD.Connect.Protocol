using System;
using ICD.Connect.Protocol.Ports.IoPort;

namespace ICD.Connect.Protocol.Mock.Ports.IoPort
{
	public sealed class MockIoPort : AbstractIoPort<MockIoPortSettings>
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
		/// Sets the configuration mode.
		/// </summary>
		public override void SetConfiguration(eIoPortConfiguration configuration)
		{
			Configuration = configuration;
		}

		/// <summary>
		/// Sets the digital output state.
		/// </summary>
		/// <param name="digitalOut"></param>
		public override void SetDigitalOut(bool digitalOut)
		{
			if (Configuration != eIoPortConfiguration.DigitalOut)
				throw new InvalidOperationException("Not in digital output mode");

			DigitalOut = digitalOut;
		}
	}
}
