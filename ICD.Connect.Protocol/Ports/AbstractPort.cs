using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Json;
using ICD.Common.Utils.Xml;
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
		/// Does nothing if DebugRx is false.
		/// </summary>
		/// <param name="data"></param>
		protected void PrintRx(string data)
		{
			switch (DebugRx)
			{
				case eDebugMode.Off:
					break;
				case eDebugMode.Ascii:
					PrintData(data, "RX(Ascii)");
					break;
				case eDebugMode.Hex:
					PrintData(StringUtils.ToHexLiteral(data), "RX(Hex)");
					break;
				case eDebugMode.MixedAsciiHex:
					PrintData(StringUtils.ToMixedReadableHexLiteral(data), "RX(Mixed)");
					break;
				case eDebugMode.Xml:
					PrintData(XmlUtils.Format(data), "RX(Xml)");
					break;
				case eDebugMode.Json:
					PrintData(JsonUtils.Format(data), "RX(Json)");
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Formats and prints the transmitted data to the console.
		/// Does nothing if DebugTx is false.
		/// </summary>
		/// <param name="data"></param>
		protected void PrintTx(string data)
		{
			switch (DebugTx)
			{
				case eDebugMode.Off:
					break;
				case eDebugMode.Ascii:
					PrintData(data, "TX(Ascii)");
					break;
				case eDebugMode.Hex:
					PrintData(StringUtils.ToHexLiteral(data), "TX(Hex)");
					break;
				case eDebugMode.MixedAsciiHex:
					PrintData(StringUtils.ToMixedReadableHexLiteral(data), "TX(Mixed)");
					break;
				case eDebugMode.Xml:
					PrintData(XmlUtils.Format(data), "TX(Xml)");
					break;
				case eDebugMode.Json:
					PrintData(JsonUtils.Format(data), "TX(Json)");
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Formats and prints the data to the console.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="context"></param>
		private void PrintData(string data, string context)
		{
			data = data.Replace("\n", "\\n");
			data = data.Replace("\r", "\\r");

			IcdConsole.Print("{0} {1} - {2}", this, context, data);
			IcdConsole.PrintLine(string.Empty);
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

			yield return new EnumConsoleCommand<eDebugMode>("DebugMode",
															p =>
															{
																SetTxDebugMode(p);
																SetRxDebugMode(p);
															});


			yield return new EnumConsoleCommand<eDebugMode>("DebugModeTx", p => SetTxDebugMode(p));
			yield return new EnumConsoleCommand<eDebugMode>("DebugModeRx", p => SetRxDebugMode(p));
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
