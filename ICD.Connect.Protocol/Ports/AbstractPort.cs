using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices;

namespace ICD.Connect.Protocol.Ports
{
	/// <summary>
	/// Base class for all ports.
	/// </summary>
	public abstract class AbstractPort<T> : AbstractDeviceBase<T>, IPort
		where T : IPortSettings, new()
	{
		#region Properties

		/// <summary>
		/// When enabled prints the received data to the console.
		/// </summary>
		[PublicAPI]
		public bool DebugRx { get; set; }

		/// <summary>
		/// When enabled prints the transmitted data to the console.
		/// </summary>
		[PublicAPI]
		public bool DebugTx { get; set; }

		#endregion

		#region Private Methods

		/// <summary>
		/// Formats and prints the received data to the console.
		/// Does nothing if DebugRx is false.
		/// </summary>
		/// <param name="data"></param>
		protected void PrintRx(string data)
		{
			if (DebugRx)
				PrintData(data, "RX");
		}

		/// <summary>
		/// Formats and prints the transmitted data to the console.
		/// Does nothing if DebugTx is false.
		/// </summary>
		/// <param name="data"></param>
		protected void PrintTx(string data)
		{
			if (DebugTx)
				PrintData(data, "TX");
		}

		/// <summary>
		/// Formats and prints the data to the console.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="context"></param>
		private void PrintData(string data, string context)
		{
			IcdConsole.Print("{0} ID:{1} {2} - {3}", GetType().Name, Id, context, data);
			IcdConsole.PrintLine("");
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

	        addRow("Debug Rx", DebugRx);
	        addRow("Debug Tx", DebugTx);
	    }

	    /// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new ConsoleCommand("EnableDebug",
			                                "Starts printing both TX and RX data to console",
			                                () =>
			                                {
				                                DebugTx = true;
				                                DebugRx = true;
			                                });

			yield return new ConsoleCommand("DisableDebug",
			                                "Stops printing both TX and RX data to console",
			                                () =>
			                                {
				                                DebugTx = false;
				                                DebugRx = false;
			                                });

			yield return new ConsoleCommand("ToggleDebugTx", "When enabled prints TX tx data to console",
			                                () => DebugTx = !DebugTx);
			yield return new ConsoleCommand("ToggleDebugRx", "When enabled prints RX data to console",
			                                () => DebugRx = !DebugRx);
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
