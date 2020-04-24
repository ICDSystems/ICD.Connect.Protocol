using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ICD.Common.Logging.LoggingContexts;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Protocol.Ports
{
	/// <summary>
	/// AbstractSerialPort is the base class for ICD serial ports.
	/// </summary>
	public abstract class AbstractSerialPort<T> : AbstractConnectablePort<T>, ISerialPort
		where T : ISerialPortSettings, new()
	{
		/// <summary>
		/// Rasied when the port receives data from the remote endpoint.
		/// </summary>
		public event EventHandler<StringEventArgs> OnSerialDataReceived;

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnSerialDataReceived = null;

			base.DisposeFinal(disposing);
		}

		/// <summary>
		/// Sends the string data to the port. Wraps SendFinal to handle connection status.
		/// </summary>
		/// <param name="data">data to send</param>
		public bool Send(string data)
		{
			if (data == null)
				throw new ArgumentNullException("data");

			if (IsDisposed)
				throw new ObjectDisposedException(GetType().Name, string.Format("{0} is disposed", this));

			try
			{
				if (IsConnected)
					return SendFinal(data);

				Logger.Log(eSeverity.Error, "Unable to send - Port is not connected.");
				return false;
			}
			finally
			{
				UpdateIsConnectedState();
			}
		}

		/// <summary>
		/// Sends the data to the remote endpoint.
		/// </summary>
		protected abstract bool SendFinal(string data);

		/// <summary>
		/// Raises the OnSerialDataReceived event.
		/// </summary>
		/// <param name="data"></param>
		public virtual void Receive(string data)
		{
			try
			{
				OnSerialDataReceived.Raise(this, new StringEventArgs(data));
			}
			catch (Exception e)
			{
				Logger.Log(eSeverity.Error, e, "Exception handling received data - {0}", e.Message);
			}
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

			addRow("Is Connected", IsConnected);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new ParamsConsoleCommand("Send", "Sends the serial data to the port", a => ConsoleSend(a));
			yield return new ParamsConsoleCommand("Receive", "Mocks incoming data from the port", a => ConsoleReceive(a));
		}

		/// <summary>
		/// Workaround to avoid "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		/// <summary>
		/// Shim to send data from console command.
		/// </summary>
		/// <param name="data"></param>
		private void ConsoleSend(params string[] data)
		{
			if (data == null)
				throw new ArgumentNullException("data");

			// Rejoin the parameters
			string command = string.Join(" ", data);

			// Replace escape codes with the actual characters
			command = Regex.Unescape(command);

			Send(command);
		}

		/// <summary>
		/// Shim to receive data from console command.
		/// </summary>
		/// <param name="data"></param>
		private void ConsoleReceive(params string[] data)
		{
			if (data == null)
				throw new ArgumentNullException("data");

			// Rejoin the parameters
			string command = string.Join(" ", data);

			// Replace escape codes with the actual characters
			command = Regex.Unescape(command);

			Receive(command);
		}

		#endregion
	}
}
