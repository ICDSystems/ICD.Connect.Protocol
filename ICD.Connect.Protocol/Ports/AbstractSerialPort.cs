﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ICD.Common.Utils;
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
					Log(eSeverity.Notice, "Port is not connected. Attempting to reconnect.");
					Connect();
				}

				if (!IsConnected)
				{
					Log(eSeverity.Error, "Port is not connected. Reconnection failed.");
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

		#endregion
	}
}
