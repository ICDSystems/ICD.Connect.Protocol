using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Settings;
using ICD.Connect.Settings;

namespace ICD.Connect.Protocol.Ports.IrPort
{
	public abstract class AbstractIrPort<T> : AbstractPort<T>, IIrPort
		where T : IIrPortSettings, new()
	{
		#region Properties

		/// <summary>
		/// Gets the path to the loaded IR driver.
		/// </summary>
		public abstract string DriverPath { get; }

		/// <summary>
		/// Gets/sets the default pulse time in milliseconds for a PressAndRelease.
		/// </summary>
		public abstract ushort PulseTime { get; set; }

		/// <summary>
		/// Gets/sets the default time in milliseconds between PressAndRelease commands.
		/// </summary>
		public abstract ushort BetweenTime { get; set; }

		/// <summary>
		/// Gets the IR Driver configuration properties.
		/// </summary>
		public abstract IIrDriverProperties IrDriverProperties { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Loads the driver from the given path.
		/// </summary>
		/// <param name="path"></param>
		public abstract void LoadDriver(string path);

		/// <summary>
		/// Gets the loaded IR commands.
		/// </summary>
		/// <returns></returns>
		public abstract IEnumerable<string> GetCommands();

		/// <summary>
		/// Begin sending the command.
		/// </summary>
		/// <param name="command"></param>
		public abstract void Press(string command);

		/// <summary>
		/// Stop sending the current command.
		/// </summary>
		public abstract void Release();

		/// <summary>
		/// Sends the command for the default pulse time.
		/// </summary>
		/// <param name="command"></param>
		public abstract void PressAndRelease(string command);

		/// <summary>
		/// Send the command for the given number of milliseconds.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="pulseTime"></param>
		public abstract void PressAndRelease(string command, ushort pulseTime);

		/// <summary>
		/// Sends the command for the given number of milliseconds.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="pulseTime"></param>
		/// <param name="betweenTime"></param>
		public abstract void PressAndRelease(string command, ushort pulseTime, ushort betweenTime);

		/// <summary>
		/// Applies the given device configuration properties to the port.
		/// </summary>
		/// <param name="properties"></param>
		public void ApplyDeviceConfiguration(IIrDriverProperties properties)
		{
			if (properties == null)
				throw new ArgumentNullException("properties");

			// Port supercedes device configuration
			IIrDriverProperties config = IrDriverProperties.Superimpose(properties);

			ApplyConfiguration(config);
		}

		/// <summary>
		/// Applies the IR driver configuration to the port.
		/// </summary>
		public void ApplyConfiguration()
		{
			ApplyConfiguration(IrDriverProperties);
		}

		/// <summary>
		/// Applies the given configuration properties to the port.
		/// </summary>
		/// <param name="properties"></param>
		public void ApplyConfiguration(IIrDriverProperties properties)
		{
			if (properties == null)
				throw new ArgumentNullException("properties");

			if (properties.IrPulseTime.HasValue)
				PulseTime = properties.IrPulseTime.Value;

			if (properties.IrBetweenTime.HasValue)
				BetweenTime = properties.IrBetweenTime.Value;

			if (properties.IrDriverPath != null)
				LoadDriver(properties.IrDriverPath);
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			IrDriverProperties.ClearIrProperties();
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(T settings)
		{
			base.CopySettingsFinal(settings);

			settings.Copy(IrDriverProperties);
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(T settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			IrDriverProperties.Copy(settings);
		}

		#endregion

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Driver Path", DriverPath);
			addRow("Pulse Time", PulseTime);
			addRow("Between Time", BetweenTime);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<ushort>("SetPulseTime",
			                                               "Sets the number of milliseconds to hold each command",
			                                               u => PulseTime = u);
			yield return new GenericConsoleCommand<ushort>("SetBetweenTime",
			                                               "Sets the number of milliseconds between each command",
			                                               u => BetweenTime = u);
			yield return new GenericConsoleCommand<string>("Press", "Press <COMMAND>", p => Press(p));
			yield return new ConsoleCommand("Release", "Releases the current command", () => Release());
			yield return new GenericConsoleCommand<string>("PressAndRelease",
			                                               "Presses and releases the given command",
			                                               a => PressAndRelease(a));
			yield return
				new GenericConsoleCommand<string>("LoadDriver", "Loads the driver at the given path", a => LoadDriver(a));
			yield return new ConsoleCommand("PrintCommands", "Prints the available commands", () => PrintCommands());
		}

		/// <summary>
		/// Workaround to avoid "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		private string PrintCommands()
		{
			TableBuilder builder = new TableBuilder("Command");

			foreach (string command in GetCommands())
				builder.AddRow(command);

			return builder.ToString();
		}

		#endregion
	}
}
