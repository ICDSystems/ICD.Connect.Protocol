using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
	public abstract class AbstractSerialPort<T> : AbstractPort<T>, ISerialPort
		where T : ISerialPortSettings, new()
	{
		/// <summary>
		/// Rasied when the port receives data from the remote endpoint.
		/// </summary>
		public event EventHandler<StringEventArgs> OnSerialDataReceived;

		/// <summary>
		/// Raised when the port connection status changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnConnectedStateChanged;

		private bool m_IsConnected;

		#region Properties

		/// <summary>
		/// Returns the connection state of the port.
		/// </summary>
		public virtual bool IsConnected
		{
			get { return m_IsConnected; }
			protected set
			{
				if (value == m_IsConnected)
					return;

				m_IsConnected = value;

				Log(eSeverity.Informational, "Connected state changed to {0}", m_IsConnected);

				UpdateCachedOnlineStatus();

				OnConnectedStateChanged.Raise(this, new BoolEventArgs(m_IsConnected));
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Connects to the end point.
		/// </summary>
		public abstract void Connect();

		/// <summary>
		/// Disconnects from the end point.
		/// </summary>
		public abstract void Disconnect();

		/// <summary>
		/// Returns the connection state of the port
		/// </summary>
		/// <returns></returns>
		protected abstract bool GetIsConnectedState();

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return GetIsConnectedState();
		}

		/// <summary>
		/// Queries the connection state of the port and updates the IsConnected property.
		/// </summary>
		protected void UpdateIsConnectedState()
		{
			IsConnected = GetIsConnectedState();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnSerialDataReceived = null;
			OnConnectedStateChanged = null;

			Disconnect();

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

				Log(eSeverity.Error, "Unable to send - Port is not connected.");
				return false;
			}
			finally
			{
				UpdateIsConnectedState();
			}
		}

		/// <summary>
		/// Implements the actual sending logic. Wrapped by Send to handle connection status.
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
				Logger.AddEntry(eSeverity.Error, e, "{0} - Exception handling received data - {1}", this, e.Message);
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

			yield return new ConsoleCommand("Connect", "Connects to the physical endpoint", () => Connect());
			yield return new ConsoleCommand("Disconnect", "Disconnects from the physical endpoint", () => Disconnect());
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
