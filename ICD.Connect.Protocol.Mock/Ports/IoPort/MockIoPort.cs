using System;
using System.Collections.Generic;
using ICD.Connect.API.Commands;
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

		private void SetDigitalInput(bool state)
		{
			if (Configuration != eIoPortConfiguration.DigitalIn)
				throw new InvalidOperationException("Not in digital input mode");

			DigitalIn = state;
		}

		private void SetAnalogInput(ushort value)
		{
			if (Configuration != eIoPortConfiguration.AnalogIn)
				throw new InvalidOperationException("Not in analog input mode");
			
			AnalogIn = value;
		}
		
		#region Console

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<bool>("SetDigitalIn", "SetDigitalIn (bool) - Sets the state of the digital input, true|false",
				state => SetDigitalInput(state));
			yield return new GenericConsoleCommand<ushort>("SetAnalogIn",
				"SetAnalogIn (ushort) - Sets the value of the analog input, 0 - 65535", value => SetAnalogInput(value));
		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
