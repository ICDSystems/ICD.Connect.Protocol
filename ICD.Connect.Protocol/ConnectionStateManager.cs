using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Devices.EventArguments;
using ICD.Connect.Protocol.Heartbeat;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol
{
	public delegate void ConfigurePortCallback(ISerialPort port);

	public sealed class ConnectionStateManager : IConnectable, IDisposable
	{
		public event EventHandler<BoolEventArgs> OnConnectedStateChanged;
		public event EventHandler<StringEventArgs> OnSerialDataReceived;
		public event EventHandler<BoolEventArgs> OnIsOnlineStateChanged;

		private object m_Parent;

		private ISerialPort m_Port;
		private bool m_IsConnected;
		private bool m_IsOnline;

		private ILoggerService Logger { get { return ServiceProvider.TryGetService<ILoggerService>(); } }

		public ConfigurePortCallback ConfigurePort { get; set; }

		public Heartbeat.Heartbeat Heartbeat { get; private set; }

		public bool IsConnected
		{
			get
			{
				return m_IsConnected;
			}
			private set
			{
				if (value == m_IsConnected)
					return;
				
				m_IsConnected = value;

				OnConnectedStateChanged.Raise(this, new BoolEventArgs(m_IsConnected));
			}
		}

		public bool IsOnline
		{
			get
			{
				return m_IsOnline;
			}
			private set
			{
				if (value == m_IsOnline)
					return;

				m_IsOnline = value;

				OnIsOnlineStateChanged.Raise(this, new BoolEventArgs(m_IsOnline));
			}
		}

		public int? PortNumber { get { return m_Port == null ? (int?)null : m_Port.Id; } }

		public ConnectionStateManager(object connectable)
		{
			m_Parent = connectable;
			Heartbeat = new Heartbeat.Heartbeat(this);
		}

		public void Dispose()
		{
			Heartbeat.Dispose();

			SetPort(null);

			ConfigurePort = null;
			m_Parent = null;
		}

		/// <summary>
		/// Sets the port for communicating with the remote endpoint.
		/// </summary>
		/// <param name="port"></param>
		[PublicAPI]
		public void SetPort(ISerialPort port)
		{
			if (port == m_Port)
				return;

			if (ConfigurePort != null)
				ConfigurePort(port);

			Heartbeat.StopMonitoring();

			if (m_Port != null)
				Disconnect();

			Unsubscribe(m_Port);
			m_Port = port;
			Subscribe(m_Port);

			if (m_Port != null)
				Heartbeat.StartMonitoring();

			IsOnline = m_Port != null && m_Port.IsOnline;
			IsConnected = m_Port != null && m_Port.IsConnected;
		}

		[PublicAPI]
		public void Connect()
		{
			if (m_Port == null)
			{
				Log(eSeverity.Critical, "Unable to connect, port is null");
				return;
			}

			m_Port.Connect();
		}

		[PublicAPI]
		public void Disconnect()
		{
			if (m_Port == null)
			{
				Log(eSeverity.Critical, "Unable to disconnect, port is null");
				return;
			}

			m_Port.Disconnect();
		}

		public bool Send(string data)
		{
			if (!m_Port.IsConnected)
			{
				Log(eSeverity.Error, "Unable to send command to {0}, port is not connected", m_Parent);
				return false;
			}
			return m_Port.Send(data);
		}

		/// <summary>
		/// Overrides tostring to provide context as to what device this is wrapping, for logging.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return string.Format("({0}){1}", GetType().Name, m_Parent);
		}

		/// <summary>
		/// Subscribes to the port events.
		/// </summary>
		/// <param name="port"></param>
		private void Subscribe(ISerialPort port)
		{
			if (port == null)
				return;

			port.OnSerialDataReceived += WrappedPortOnSerialDataReceived;
			port.OnConnectedStateChanged += WrappedPortOnConnectionStatusChanged;
			port.OnIsOnlineStateChanged += WrappedPortOnIsOnlineStateChanged;
		}

		/// <summary>
		/// Unsubscribe from the port events.
		/// </summary>
		/// <param name="port"></param>
		private void Unsubscribe(ISerialPort port)
		{
			if (port == null)
				return;

			port.OnSerialDataReceived -= WrappedPortOnSerialDataReceived;
			port.OnConnectedStateChanged -= WrappedPortOnConnectionStatusChanged;
			port.OnIsOnlineStateChanged -= WrappedPortOnIsOnlineStateChanged;
		}

		private void WrappedPortOnSerialDataReceived(object sender, StringEventArgs e)
		{
			OnSerialDataReceived.Raise(this, new StringEventArgs(e.Data));
		}

		private void WrappedPortOnConnectionStatusChanged(object sender, BoolEventArgs e)
		{
			IsConnected = m_Port != null && m_Port.IsConnected;
		}

		private void WrappedPortOnIsOnlineStateChanged(object sender, DeviceBaseOnlineStateApiEventArgs eventArgs)
		{
			IsOnline = m_Port != null && m_Port.IsOnline;
		}

		private void Log(eSeverity severity, string message)
		{
			Logger.AddEntry(severity, "{0} - {1}", this, message);
		}

		private void Log(eSeverity severity, string message, params object[] args)
		{
			Logger.AddEntry(severity, string.Format("{0} - {1}", this, string.Format(message, args)));
		}
	}
}