using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Timers;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Protocol.Network.Broadcast
{
	public delegate void BroadcastCallback(object data);

	public abstract class RecurringBroadcaster : IDisposable, IConsoleNode
	{
		private const long DEFAULT_INTERVAL = 30 * 1000;

		public event EventHandler OnBroadcasting;

		/// <summary>
		/// How often to broadcast the available crosspoints.
		/// </summary>
		private readonly long m_BroadcastInterval;

		private readonly SafeTimer m_BroadcastTimer;

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

		/// <summary>
		/// Constructor.
		/// </summary>
		protected RecurringBroadcaster()
			: this(DEFAULT_INTERVAL)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="broadcastInterval"></param>
		protected RecurringBroadcaster(long broadcastInterval)
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
			OnBroadcasting = null;

			m_BroadcastTimer.Dispose();
		}

		public void Broadcast()
		{
			OnBroadcasting.Raise(this);

			BroadcastCallback callback = SendBroadcastData;
			if (callback != null)
				callback(BroadcastData);
		}

		protected internal abstract void HandleIncomingBroadcast(BroadcastData broadcastData);

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
	public sealed class RecurringBroadcaster<T> : RecurringBroadcaster
	{
		/// <summary>
		/// Raised when a broadcast is received.
		/// </summary>
		public event EventHandler<BroadcastEventArgs<T>> OnBroadcastReceived;

		private T m_BroadcastData;

		protected override object BroadcastData { get { return m_BroadcastData; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		public RecurringBroadcaster()
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="broadcastInterval"></param>
		public RecurringBroadcaster(long broadcastInterval)
			: base(broadcastInterval)
		{
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public override void Dispose()
		{
			OnBroadcastReceived = null;
			
			base.Dispose();

			m_BroadcastData = default(T);
		}

		protected internal override void HandleIncomingBroadcast(BroadcastData broadcastData)
		{
			if (broadcastData.Data is T)
				OnBroadcastReceived.Raise(this, new BroadcastEventArgs<T>(new BroadcastData<T>(broadcastData)));
			else
				throw new InvalidOperationException(string.Format("Broadcast does not have data of type {0}", typeof(T)));
		}

		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Broadcast Data", m_BroadcastData);
		}

		/// <summary>
		/// Update the data that will be broadcast.
		/// </summary>
		/// <param name="info"></param>
		[PublicAPI]
		public void UpdateData(T info)
		{
			m_BroadcastData = info;
		}

		#endregion
	}
}
