using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.EventArguments;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Protocol.Ports
{
	/// <summary>
	/// AbstractSerialPort is the base class for RSD serial ports.
	/// </summary>
	public abstract class AbstractSerialPort<T> : AbstractPort<T>, ISerialPort
		where T : IPortSettings, new()
	{
		public event EventHandler<StringEventArgs> OnSerialDataReceived;
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
			if (IsDisposed)
				throw new ObjectDisposedException(GetType().Name);

			return ConnectAndSend(data, SendFinal);
		}

		/// <summary>
		/// Implements the actual sending logic. Wrapped by Send to handle connection status.
		/// </summary>
		protected abstract bool SendFinal(string data);

		/// <summary>
		/// Attempts to connect if not currently connected and calls the send function.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="sendFunction"></param>
		/// <returns></returns>
		protected bool ConnectAndSend(string data, Func<string, bool> sendFunction)
		{
			try
			{
				if (!IsConnected)
				{
					Logger.AddEntry(eSeverity.Notice, "{0}.Send - Port is not connected. Attempting to reconnect.", GetType().Name);
					Connect();
				}

				if (!IsConnected)
				{
					Logger.AddEntry(eSeverity.Error, "{0}.Send - Port is not connected. Reconnection failed.", GetType().Name);
					return false;
				}

				return sendFunction(data);
			}
			finally
			{
				UpdateIsConnectedState();
			}
		}

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
				Logger.AddEntry(eSeverity.Error, e, "Port data received exception - {0}", e.Message);
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
			yield return new ParamsConsoleCommand("SendHex", "Sends hex data in the format \\xFF\\xFF...", b => SendHex(b));
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
			Send(string.Join(" ", data));
		}

		/// <summary>
		/// Shim to send hex data from console command.
		/// </summary>
		/// <param name="data"></param>
		private void SendHex(params string[] data)
		{
			foreach (string literal in data.Select(item => StringUtils.FromHexLiteral(item)))
				Send(literal);
		}

		#endregion
	}
}
