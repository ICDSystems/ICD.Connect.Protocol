using ICD.Connect.Protocol.Ports.IrPort;

namespace ICD.Connect.Protocol.Mock.Ports.IrPort
{
	public sealed class MockIrPort : AbstractIrPort<MockIrPortSettings>
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
		/// Gets/sets the default pulse time in milliseconds for a PressAndRelease.
		/// </summary>
		public override ushort PulseTime { get; set; }

		/// <summary>
		/// Gets/sets the default time in milliseconds between PressAndRelease commands.
		/// </summary>
		public override ushort BetweenTime { get; set; }

		/// <summary>
		/// Loads the driver from the given path.
		/// </summary>
		/// <param name="path"></param>
		public override void LoadDriver(string path)
		{
		}

		/// <summary>
		/// Begin sending the command.
		/// </summary>
		/// <param name="command"></param>
		public override void Press(string command)
		{
		}

		/// <summary>
		/// Stop sending the current command.
		/// </summary>
		public override void Release()
		{
		}

		/// <summary>
		/// Sends the command for the default pulse time.
		/// </summary>
		/// <param name="command"></param>
		public override void PressAndRelease(string command)
		{
		}

		/// <summary>
		/// Send the command for the given number of milliseconds.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="pulseTime"></param>
		public override void PressAndRelease(string command, ushort pulseTime)
		{
		}

		/// <summary>
		/// Sends the command for the given number of milliseconds.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="pulseTime"></param>
		/// <param name="betweenTime"></param>
		public override void PressAndRelease(string command, ushort pulseTime, ushort betweenTime)
		{
		}
	}
}
