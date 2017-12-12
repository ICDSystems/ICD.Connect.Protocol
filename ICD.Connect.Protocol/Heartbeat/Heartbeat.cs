using System;
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

        private static readonly long[] RAMP_MS_DEFAULT =
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

        public Heartbeat(IConnectable instance) : this(instance, MAX_INTERVAL_MS_DEFAULT, RAMP_MS_DEFAULT) { }

        public Heartbeat(IConnectable instance, long maxIntervalMs) : this(instance, maxIntervalMs, RAMP_MS_DEFAULT) { }

        public Heartbeat(IConnectable instance, long[] rampIntervalMs) : this(instance, MAX_INTERVAL_MS_DEFAULT, rampIntervalMs) { }

        public Heartbeat(IConnectable instance, long maxIntervalMs, long[] rampIntervalMs)
        {
            m_Instance = instance;
            m_MaxIntervalMs = maxIntervalMs;
            m_RampIntervalMs = rampIntervalMs.ToArray();

            instance.OnConnectedStateChanged += InstanceOnConnectedStateChanged;
            m_Timer = SafeTimer.Stopped(TimerCallback);
        }

        public void StartMonitoring()
        {
            ThreadingUtils.SafeInvoke(() => m_Timer.Reset(0));
            m_MonitoringActive = true;
        }

        public void StopMonitoring()
        {
            m_Timer.Stop();
            m_MonitoringActive = false;
        }

        private void TimerCallback()
        {
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
            m_Timer.Dispose();

            m_Instance.OnConnectedStateChanged -= InstanceOnConnectedStateChanged;
            m_Instance.Disconnect();

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

                if (m_ConnectAttempts < m_RampIntervalMs.Length)
                {
                    Logger.AddEntry(eSeverity.Warning,
                                    m_ConnectAttempts == 0
                                        ? "{0} lost connection. Attempting to reconnect. Attempted {1} time."
                                        : "{0} lost connection. Attempting to reconnect. Attempted {1} times.",
                                    m_Instance,
                                    m_ConnectAttempts + 1);
                    m_Timer.Reset(m_RampIntervalMs[m_ConnectAttempts], m_RampIntervalMs[m_ConnectAttempts]);
                    m_ConnectAttempts++;
                }
                else
                {
                    Logger.AddEntry(eSeverity.Error, "{0} lost connection. Attempting to reconnect. Attempted {1} times.", m_Instance, m_ConnectAttempts + 1);
                    m_Timer.Reset(m_MaxIntervalMs, m_MaxIntervalMs);
                }

                m_Instance.Connect();
            }
            finally
            {
                m_Connecting = false;
            }
        }


    }
}