using ICD.Common.Properties;
using ICD.Common.Utils.Timers;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Protocol.Network.Broadcast.Broadcasters
{
	public abstract class AbstractRecurringBroadcaster : AbstractBroadcaster
	{
		private const long DEFAULT_INTERVAL = 30 * 1000;

		/// <summary>
		/// How often to broadcast the available crosspoints.
		/// </summary>
		private readonly long m_BroadcastInterval;

		private readonly SafeTimer m_BroadcastTimer;

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractRecurringBroadcaster()
			: this(DEFAULT_INTERVAL)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="broadcastInterval"></param>
		protected AbstractRecurringBroadcaster(long broadcastInterval)
		{
			m_BroadcastInterval = broadcastInterval;
			m_BroadcastTimer = new SafeTimer(Broadcast, m_BroadcastInterval, m_BroadcastInterval);
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public override void Dispose()
		{
			base.Dispose();

			m_BroadcastTimer.Dispose();
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

			addRow("Broadcast Interval (seconds)", m_BroadcastInterval / 1000.0f);
		}

		#endregion
	}

	public sealed class RecurringBroadcaster<T> : AbstractRecurringBroadcaster
	{
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
			base.Dispose();

			m_BroadcastData = default(T);
		}

		public override void SetBroadcastData(object data)
		{
			SetBroadcastData((T)data);
		}

		/// <summary>
		/// Update the data that will be broadcast.
		/// </summary>
		/// <param name="data"></param>
		[PublicAPI]
		public void SetBroadcastData(T data)
		{
			m_BroadcastData = data;
		}

		#endregion
	}
}
