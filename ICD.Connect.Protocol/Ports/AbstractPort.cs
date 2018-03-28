using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices;
using ICD.Connect.Protocol.Utils;

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
		public eDebugMode DebugRx { get; set; }

		/// <summary>
		/// When enabled prints the transmitted data to the console.
		/// </summary>
		[PublicAPI]
		public eDebugMode DebugTx { get; set; }

		#endregion

		#region Private Methods

		/// <summary>
		/// Formats and prints the received data to the console.
		/// </summary>
		/// <param name="data"></param>
		protected void PrintRx(string data)
		{
			DebugUtils.PrintRx(this, DebugRx, data);
		}

		/// <summary>
		/// Formats and prints the received data to the console.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="data"></param>
		protected void PrintRx(string context, string data)
		{
			DebugUtils.PrintRx(this, DebugRx, context, data);
		}

		/// <summary>
		/// Formats and prints the transmitted data to the console.
		/// </summary>
		/// <param name="data"></param>
		protected void PrintTx(string data)
		{
			DebugUtils.PrintTx(this, DebugTx, data);
		}

		/// <summary>
		/// Formats and prints the transmitted data to the console.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="data"></param>
		protected void PrintTx(string context, string data)
		{
			DebugUtils.PrintTx(this, DebugTx, context, data);
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

			yield return new ConsoleCommand("EnableDebug", "Sets debug mode for TX/RX to Ascii",
			                                () =>
			                                {
				                                SetTxDebugMode(eDebugMode.Ascii);
				                                SetRxDebugMode(eDebugMode.Ascii);
			                                });

			yield return new ConsoleCommand("DisableDebug", "Sets debug mode for TX/RX to Off",
			                                () =>
			                                {
				                                SetTxDebugMode(eDebugMode.Off);
				                                SetRxDebugMode(eDebugMode.Off);
			                                });

			yield return new EnumConsoleCommand<eDebugMode>("SetDebugMode",
			                                                p =>
			                                                {
				                                                SetTxDebugMode(p);
				                                                SetRxDebugMode(p);
			                                                });

			yield return new EnumConsoleCommand<eDebugMode>("SetDebugModeTx", p => SetTxDebugMode(p));
			yield return new EnumConsoleCommand<eDebugMode>("SetDebugModeRx", p => SetRxDebugMode(p));
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		private void SetTxDebugMode(eDebugMode mode)
		{
			DebugTx = mode;
		}

		private void SetRxDebugMode(eDebugMode mode)
		{
			DebugRx = mode;
		}

		#endregion
	}
}
