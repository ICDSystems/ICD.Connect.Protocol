using System;
using System.Collections.Generic;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Timers;

namespace ICD.Connect.Protocol.Network.Broadcast
{
	public delegate void BroadcastHandler(object data);

	public abstract class RecurringBroadcast : IDisposable, IConsoleNode
	{
		/// <summary>
		/// How often to broadcast the available crosspoints.
		/// </summary>
		private readonly long m_BroadcastInterval = 30 * 1000;

		private readonly SafeTimer m_BroadcastTimer;

		public BroadcastHandler SendBroadcastData { get; set; }

		#region Properties

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public string ConsoleName { get { return GetType().Name; } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public string ConsoleHelp { get { return "Responsible for broadcasting information to the network"; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		protected RecurringBroadcast() : this(30 * 1000)
		{
		}

		protected RecurringBroadcast(long broadcastInterval)
		{
			m_BroadcastInterval = broadcastInterval;
			m_BroadcastTimer = new SafeTimer(Broadcast, m_BroadcastInterval, m_BroadcastInterval);
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public virtual void Dispose()
		{
			m_BroadcastTimer.Dispose();
		}

		protected abstract void Broadcast();

		internal abstract void HandleIncomingBroadcast(Broadcast broadcast);

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
			addRow("Broadcast Interval (seconds)", m_BroadcastInterval / 1000.0f);
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

	/// <summary>
	/// The AdvertisementManager is responsible for broadcasting the local crosspoints,
	/// and discovering remote crosspoints.
	/// </summary>
	public sealed class RecurringBroadcast<T> : RecurringBroadcast
	{
		/// <summary>
		/// Raised when crosspoints are discovered.
		/// </summary>
		public event EventHandler<BroadcastEventArgs<T>> OnBroadcastReceived;

		public event EventHandler OnBroadcasting;

		private T m_BroadcastData;

		public RecurringBroadcast()
		{
		}

		public RecurringBroadcast(long broadcastInterval) : base(broadcastInterval)
		{
		}

		#region Methods

		public override void Dispose()
		{
			base.Dispose();
			StopBroadcasting();
		}

		protected override void Broadcast()
		{
			OnBroadcasting.Raise(this);
			BroadcastHandler handler = SendBroadcastData;
			if (handler != null)
				handler.Invoke(m_BroadcastData);
		}

		internal override void HandleIncomingBroadcast(Broadcast broadcast)
		{
			if (broadcast.Data is T)
				OnBroadcastReceived.Raise(this, new BroadcastEventArgs<T>(new Broadcast<T>(broadcast)));
			else
				throw new InvalidOperationException(string.Format("Broadcast does not have data of type {0}", typeof(T)));
		}

		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);
			addRow("Broadcast Data", m_BroadcastData.ToString());
		}

		/// <summary>
		/// Update the data that will be broadcasted
		/// </summary>
		/// <param name="info"></param>
		[PublicAPI]
		public void UpdateData(T info)
		{
			if (info != null && info.Equals(m_BroadcastData))
				return;

			m_BroadcastData = info;
		}

		/// <summary>
		/// Stops advertising equipment crosspoints for remote discovery.
		/// </summary>
		[PublicAPI]
		public void StopBroadcasting()
		{
			m_BroadcastData = default(T);
		}

		#endregion
	}
}
