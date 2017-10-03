using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Protocol.Ports.RelayPort
{
	public abstract class AbstractRelayPort<T> : AbstractPort<T>, IRelayPort
		where T : IRelayPortSettings, new()
	{
		public event EventHandler<BoolEventArgs> OnClosedStateChanged;

		private bool m_Closed;

		/// <summary>
		/// Get the state of the relay.
		/// </summary>
		public bool Closed
		{
			get { return m_Closed; }
			protected set
			{
				if (value == m_Closed)
					return;

				m_Closed = value;

				OnClosedStateChanged.Raise(this, new BoolEventArgs(m_Closed));
			}
		}

		/// <summary>
		/// Open the relay
		/// </summary>
		public abstract void Open();

		/// <summary>
		/// Close the relay
		/// </summary>
		public abstract void Close();

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Closed", Closed);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new ConsoleCommand("Open", "Opens the relay", () => Open());
			yield return new ConsoleCommand("Close", "Closes the relay", () => Close());
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
