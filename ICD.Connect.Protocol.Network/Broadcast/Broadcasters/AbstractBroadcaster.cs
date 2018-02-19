using System;
using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Protocol.Network.Broadcast.Broadcasters
{
    public abstract class AbstractBroadcaster : IBroadcaster, IConsoleNode
    {
		public event EventHandler OnBroadcasting;
		public event EventHandler<BroadcastEventArgs> OnBroadcastReceived;

		#region Properties

		protected abstract object BroadcastData { get; }

		/// <summary>
		/// Called to send the broadcast.
		/// </summary>
		public BroadcastCallback SendBroadcastData { get; set; }

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public string ConsoleName { get { return GetType().Name; } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public string ConsoleHelp { get { return "Responsible for broadcasting information to the network"; } }

		#endregion

		#region Methods

		public virtual void Dispose()
		{
			OnBroadcasting = null;
			OnBroadcastReceived = null;
		}

		public abstract void SetBroadcastData(object data);

		public void Broadcast()
		{
			OnBroadcasting.Raise(this);

			BroadcastCallback callback = SendBroadcastData;
			if (callback != null)
				callback(BroadcastData);
		}

		public void HandleIncomingBroadcast(BroadcastData broadcastData)
		{
			OnBroadcastReceived.Raise(this, new BroadcastEventArgs(new BroadcastData(broadcastData)));
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public virtual IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield break;
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public virtual void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("Broadcast Data", BroadcastData);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public virtual IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new ConsoleCommand("Broadcast", "Immediately broadcasts the data to the network", () => Broadcast());
		}

		#endregion
	}
}
