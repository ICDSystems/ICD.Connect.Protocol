using System.Collections.Generic;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Protocol.Ports.IrPort
{
	public abstract class AbstractIrPort<T> : AbstractPort<T>, IIrPort
		where T : IPortSettings, new()
	{
		#region Properties

		/// <summary>
		/// Gets/sets the default pulse time in milliseconds for a PressAndRelease.
		/// </summary>
		public abstract ushort PulseTime { get; set; }

		/// <summary>
		/// Gets/sets the default time in milliseconds between PressAndRelease commands.
		/// </summary>
		public abstract ushort BetweenTime { get; set; }

		#endregion

		#region Methods

		/// <summary>
		/// Loads the driver from the given path.
		/// </summary>
		/// <param name="path"></param>
		public abstract void LoadDriver(string path);

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

		#endregion

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

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
			yield return new GenericConsoleCommand<string>("PressAndRelease",
			                                               "Presses and releases the given command",
			                                               a => PressAndRelease(a));
			yield return
				new GenericConsoleCommand<string>("LoadDriver", "Loads the driver at the given path", a => LoadDriver(a));
		}

		/// <summary>
		/// Workaround to avoid "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
