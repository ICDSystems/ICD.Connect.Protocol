using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Services;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Timers;

namespace ICD.Connect.Protocol.Heartbeat
{
    public sealed class Heartbeat : IDisposable
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
        private int m_ConnectAttempts;
        private bool m_MonitoringActive;
        private bool m_Connecting;
        private IConnectable m_Instance;


        public ILoggerService Logger
        {
            get { return ServiceProvider.GetService<ILoggerService>(); }
        }

	    public Heartbeat(IConnectable instance)
		    : this(instance, MAX_INTERVAL_MS_DEFAULT, s_RampMsDefault)
	    {
	    }

	    public Heartbeat(IConnectable instance, long maxIntervalMs)
			: this(instance, maxIntervalMs, s_RampMsDefault)
	    {
	    }

	    public Heartbeat(IConnectable instance, IEnumerable<long> rampIntervalMs)
		    : this(instance, MAX_INTERVAL_MS_DEFAULT, rampIntervalMs)
	    {
	    }

        public Heartbeat(IConnectable instance, long maxIntervalMs, IEnumerable<long> rampIntervalMs)
        {
            m_Instance = instance;
            m_MaxIntervalMs = maxIntervalMs;
            m_RampIntervalMs = rampIntervalMs.ToArray();

            m_Timer = SafeTimer.Stopped(TimerCallback);
        }

        public void StartMonitoring()
        {
            m_MonitoringActive = true;

            // Subscribe to state change events
            m_Instance.OnConnectedStateChanged += InstanceOnConnectedStateChanged;

            // Check the connection now, but in a new thread
            // This will start the timer if we are currently disconnected
            ThreadingUtils.SafeInvoke(TimerCallback);
        }

        public void StopMonitoring()
        {
            m_MonitoringActive = false;

            // Unsubscribe from events
            if (m_Instance != null)
                m_Instance.OnConnectedStateChanged -= InstanceOnConnectedStateChanged;
            
            m_Timer.Stop();
        }

        private void TimerCallback()
        {
            if (m_Instance == null)
                return;

            if (m_Instance.IsConnected)
            {
                HandleConnected();
            }
            else
            {
                HandleDisconnected();
            }
        }

        public void Dispose()
        {
            StopMonitoring();
            m_Timer.Dispose();

            m_Instance = null;
        }

        private void InstanceOnConnectedStateChanged(object sender, BoolEventArgs eventArgs)
        {
            if (!m_MonitoringActive)
            {
                return;
            }

            if (eventArgs.Data)
            {
                Logger.AddEntry(eSeverity.Notice, "{0} established connection.", m_Instance);
                HandleConnected();
            }
            else
            {
                HandleDisconnected();
            }
        }

        private void HandleConnected()
        {
            m_ConnectAttempts = 0;
            m_Connecting = false;
        }

        private void HandleDisconnected()
        {
            if (m_Connecting)
                return;

            try
            {
                m_Connecting = true;

                m_Instance.Connect();

                if (m_ConnectAttempts < m_RampIntervalMs.Length)
                {
                    Logger.AddEntry(eSeverity.Warning,
                                    m_ConnectAttempts == 0
                                        ? "{0} lost connection. Attempting to reconnect. Attempted {1} time."
                                        : "{0} lost connection. Attempting to reconnect. Attempted {1} times.",
                                    m_Instance,
                                    m_ConnectAttempts + 1);
                    m_Timer.Reset(m_RampIntervalMs[m_ConnectAttempts]);
                    m_ConnectAttempts++;
                }
                else
                {
                    Logger.AddEntry(eSeverity.Error, "{0} lost connection. Attempting to reconnect. Attempted {1} times.", m_Instance, m_ConnectAttempts + 1);
                    m_Timer.Reset(m_MaxIntervalMs);
                    m_ConnectAttempts++;
                }
            }
            finally
            {
                m_Connecting = false;
            }
        }


    }
}