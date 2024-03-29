﻿using System;
using System.Collections.Generic;
using ICD.Common.Logging.Activities;
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
	public abstract class AbstractConnectablePort<T> : AbstractPort<T>, IConnectablePort
		where T : IConnectablePortSettings, new()
	{
		/// <summary>
		/// Raised when the port connection status changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnConnectedStateChanged;

		private bool m_IsConnected;

		#region Properties

		/// <summary>
		/// Returns the connection state of the port.
		/// </summary>
		public bool IsConnected
		{
			get { return m_IsConnected; }
			protected set
			{
				try
				{
					if (value == m_IsConnected)
						return;

					m_IsConnected = value;

					eSeverity severity = m_IsConnected ? eSeverity.Informational : eSeverity.Error;

					Logger.LogSetTo(severity, "IsConnected", m_IsConnected);

					HandleIsConnectedStateChange(value);
					
					UpdateCachedOnlineStatus();

					OnConnectedStateChanged.Raise(this, new BoolEventArgs(m_IsConnected));
				}
				finally
				{
					Activities.LogActivity(m_IsConnected
						                       ? new Activity(Activity.ePriority.High, "Is Connected", "Connected",
						                                      eSeverity.Informational)
						                       : new Activity(Activity.ePriority.High, "Is Connected", "Disconnected", eSeverity.Error));
				}
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractConnectablePort()
		{
			// Initialize activities
			IsConnected = false;
		}

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
		/// Returns the connection state of the wrapped port
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
		/// Called when IsConnected state changes
		/// Called before OnlineStatus is updated, and before any events are raised
		/// </summary>
		/// <param name="isConnected"></param>
		protected virtual void HandleIsConnectedStateChange(bool isConnected)
		{
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnConnectedStateChanged = null;

			Disconnect();

			base.DisposeFinal(disposing);
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
