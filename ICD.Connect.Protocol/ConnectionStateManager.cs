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
		/// <summary>
		/// Raised when the connection state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnConnectedStateChanged;

		/// <summary>
		/// Raised when serial data is received.
		/// </summary>
		public event EventHandler<StringEventArgs> OnSerialDataReceived;

		/// <summary>
		/// Raised when the online state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnIsOnlineStateChanged;

		private readonly object m_Parent;
		private readonly Heartbeat.Heartbeat m_Heartbeat;

		private ILoggerService m_CachedLogger;

		private bool m_IsConnected;

		private bool m_IsOnline;

		#region Properties

		private ILoggerService Logger { get { return m_CachedLogger = m_CachedLogger ?? ServiceProvider.TryGetService<ILoggerService>(); } }

		public ConfigurePortCallback ConfigurePort { get; set; }

		public Heartbeat.Heartbeat Heartbeat { get { return m_Heartbeat; } }

		public bool IsConnected
		{
			get { return m_IsConnected; }
			private set
			{
				if (m_IsConnected == value)
					return;

				m_IsConnected = value;

				OnConnectedStateChanged.Raise(this, new BoolEventArgs(value));
			}
		}

		public bool IsOnline
		{
			get { return m_IsOnline; }
			private set
			{
				if(m_IsOnline == value)
					return;

				m_IsOnline = value;

				OnIsOnlineStateChanged.Raise(this, new BoolEventArgs(m_IsOnline));
			}
		}

		/// <summary>
		/// Gets the id of the current serial port.
		/// </summary>
		public int? PortNumber { get { return Port == null ? (int?)null : Port.Id; } }

		public ISerialPort Port { get; private set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="connectable"></param>
		public ConnectionStateManager(object connectable)
		{
			m_Parent = connectable;
			m_Heartbeat = new Heartbeat.Heartbeat(this);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			OnConnectedStateChanged = null;
			OnSerialDataReceived = null;
			OnIsOnlineStateChanged = null;

			m_Heartbeat.Dispose();

			SetPort(null);

			ConfigurePort = null;
		}

		#region Methods

		/// <summary>
		/// Start monitoring the state of the connection.
		/// </summary>
		public void Start()
		{
			m_Heartbeat.StartMonitoring();
		}

		/// <summary>
		/// Stop monitoring the state of the connection.
		/// </summary>
		public void Stop()
		{
			m_Heartbeat.StopMonitoring();
		}

		/// <summary>
		/// Sets the port for communicating with the remote endpoint.
		/// </summary>
		/// <param name="port"></param>
		[PublicAPI]
		public void SetPort(ISerialPort port)
		{
			SetPort(port, true);
		}

		/// <summary>
		/// Sets the port for communicating with the remote endpoint.
		/// </summary>
		/// <param name="port"></param>
		/// <param name="monitor"></param>
		[PublicAPI]
		public void SetPort(ISerialPort port, bool monitor)
		{
			if (port == Port)
				return;

			if (ConfigurePort != null)
				ConfigurePort(port);

			Stop();

			Unsubscribe(Port);
			Port = port;
			Subscribe(Port);

			if (monitor && Port != null)
				Start();

			IsConnected = port != null && port.IsConnected;
			IsOnline = port != null && port.IsOnline;
		}

		[PublicAPI]
		public void Connect()
		{
			if (Port == null)
			{
				Log(eSeverity.Critical, "Unable to connect - Port is null");
				return;
			}

			Port.Connect();
		}

		[PublicAPI]
		public void Disconnect()
		{
			if (Port == null)
			{
				Log(eSeverity.Critical, "Unable to disconnect - Port is null");
				return;
			}

			Port.Disconnect();
		}

		public bool Send(string data)
		{
			if (Port == null)
			{
				Log(eSeverity.Critical, "Unable to send data - Port is null");
				return false;
			}

			if (Port.IsConnected)
				return Port.Send(data);

			Log(eSeverity.Error, "Unable to send command - {0} is not connected", Port);
			return false;
		}

		/// <summary>
		/// Overrides tostring to provide context as to what device this is wrapping, for logging.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return string.Format("({0}){1}", GetType().Name, m_Parent);
		}

		#endregion

		#region Private Methods

		private void Log(eSeverity severity, string message)
		{
			Logger.AddEntry(severity, "{0} - {1}", this, message);
		}

		private void Log(eSeverity severity, string message, params object[] args)
		{
			Logger.AddEntry(severity, string.Format("{0} - {1}", this, string.Format(message, args)));
		}

		#endregion

		#region Port Callbacks

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
			IsConnected = Port.IsConnected;
		}

		private void WrappedPortOnIsOnlineStateChanged(object sender, DeviceBaseOnlineStateApiEventArgs eventArgs)
		{
			IsOnline = Port.IsOnline;
		}

		#endregion
	}
}
