using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;

namespace ICD.Connect.Protocol.Heartbeat
{
	public sealed class Heartbeat : IStateDisposable
	{
		private const long MAX_INTERVAL_MS_DEFAULT = 60 * 1000;

		private static readonly long[] s_RampMsDefault =
		{
			5 * 1000,
			5 * 1000,
			10 * 1000,
			10 * 1000,
			30 * 1000,
			30 * 1000
		};

		private readonly long m_MaxIntervalMs;
		private readonly long[] m_RampIntervalMs;
		private readonly SafeTimer m_Timer;
		private readonly SafeCriticalSection m_ConnectSection;

		private int m_ConnectAttempts;
		private IConnectable m_Instance;

		#region Properties

		private ILoggerService Logger { get { return ServiceProvider.GetService<ILoggerService>(); } }

		/// <summary>
		/// Returns true if this instance has been disposed.
		/// </summary>
		public bool IsDisposed { get; private set; }

		/// <summary>
		/// Returns true if currently monitoring connection state.
		/// </summary>
		public bool MonitoringActive { get; private set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="instance"></param>
		public Heartbeat(IConnectable instance)
			: this(instance, MAX_INTERVAL_MS_DEFAULT)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="maxIntervalMs"></param>
		public Heartbeat(IConnectable instance, long maxIntervalMs)
			: this(instance, maxIntervalMs, s_RampMsDefault)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="rampIntervalMs"></param>
		public Heartbeat(IConnectable instance, IEnumerable<long> rampIntervalMs)
			: this(instance, MAX_INTERVAL_MS_DEFAULT, rampIntervalMs)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="maxIntervalMs"></param>
		/// <param name="rampIntervalMs"></param>
		public Heartbeat(IConnectable instance, long maxIntervalMs, IEnumerable<long> rampIntervalMs)
		{
			m_Instance = instance;
			m_MaxIntervalMs = maxIntervalMs;
			m_RampIntervalMs = rampIntervalMs.ToArray();
			m_Timer = SafeTimer.Stopped(HandleConnectionState);
			m_ConnectSection = new SafeCriticalSection();

			Subscribe(m_Instance);
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			if (IsDisposed)
				return;

			Unsubscribe(m_Instance);

			StopMonitoring();
			m_Timer.Dispose();

			m_Instance = null;

			IsDisposed = true;
		}

		/// <summary>
		/// Starts maintaining connection state.
		/// </summary>
		public void StartMonitoring()
		{
			MonitoringActive = true;
			
			// Check after the first interval to see if we are connected
			m_Timer.Reset(s_RampMsDefault[0]);
		}

		/// <summary>
		/// Stops maintaining connection state.
		/// </summary>
		public void StopMonitoring()
		{
			MonitoringActive = false;

			m_Timer.Stop();
		}

		#endregion

		#region Instance Callbacks

		/// <summary>
		/// Subscribe to the instance events.
		/// </summary>
		/// <param name="instance"></param>
		private void Subscribe(IConnectable instance)
		{
			if (instance == null)
				return;

			instance.OnConnectedStateChanged += InstanceOnConnectedStateChanged;
		}

		/// <summary>
		/// Unsubscribe from the instance events.
		/// </summary>
		/// <param name="instance"></param>
		private void Unsubscribe(IConnectable instance)
		{
			if (instance == null)
				return;

			instance.OnConnectedStateChanged -= InstanceOnConnectedStateChanged;
		}

		/// <summary>
		/// Called when the instance connection state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void InstanceOnConnectedStateChanged(object sender, BoolEventArgs eventArgs)
		{
			if (m_Instance == null)
				return;

			if (eventArgs.Data)
				Logger.AddEntry(eSeverity.Notice, "{0} established connection.", m_Instance);
			else
				Logger.AddEntry(eSeverity.Warning, "{0} lost connection.", m_Instance);

			// Check after the first interval to start 
			if (MonitoringActive)
				m_Timer.Reset(s_RampMsDefault[0]);
		}

		private void HandleConnectionState()
		{
			if (m_Instance == null)
				return;

			if (m_Instance.IsConnected)
				HandleConnected();
			else
				HandleDisconnected();
		}

		private void HandleConnected()
		{
			m_ConnectAttempts = 0;
		}

		private void HandleDisconnected()
		{
			if (!m_ConnectSection.TryEnter())
				return;

			try
			{
				long interval;
				if (!m_RampIntervalMs.TryElementAt(m_ConnectAttempts, out interval))
					interval = m_MaxIntervalMs;

				eSeverity severity = m_ConnectAttempts >= m_RampIntervalMs.Length ? eSeverity.Error : eSeverity.Warning;

				Logger.AddEntry(severity, "{0} - Attempting to reconnect (Attempt {1}).",
				                m_Instance, m_ConnectAttempts + 1);

				m_Instance.Connect();

				if (!m_Instance.IsConnected)
				{
					m_Timer.Reset(interval);
					m_ConnectAttempts++;
				}
			}
			finally
			{
				m_ConnectSection.Leave();
			}
		}

		#endregion
	}
}
