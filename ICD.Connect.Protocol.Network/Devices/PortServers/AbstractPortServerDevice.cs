using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices;
using ICD.Connect.Devices.EventArguments;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Network.Ports.Tcp;
using ICD.Connect.Protocol.Network.Servers;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings;

namespace ICD.Connect.Protocol.Network.Devices.PortServers
{
	public abstract class AbstractPortServerDevice<TPort, TSettings> : AbstractDevice<TSettings>
		where TPort : class, IPort
		where TSettings : IPortServerDeviceSettings, new()
	{

		private TPort m_Port;

		private IcdTcpServer m_TcpServer;

		protected IcdTcpServer TcpServer { get { return m_TcpServer; } }
		private ushort? TcpServerPort { get { return m_TcpServer != null ? m_TcpServer.Port : (ushort?)null; } }

		[PublicAPI]
		public TPort Port
		{
			get { return m_Port; }
			private set
			{
				if (m_Port == value)
					return;

				Unsubscribe(m_Port);
				m_Port = value;
				Subscribe(m_Port);

				SetPortInternal(m_Port);
				
				UpdateCachedOnlineStatus();
			}
		}

		/// <summary>
		/// Called after the port is subscribed to
		/// Run any action needed to set the port here
		/// </summary>
		/// <param name="port"></param>
		protected virtual void SetPortInternal(TPort port)
		{
		}

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected sealed override bool GetIsOnlineStatus()
		{
			return Port != null && Port.IsOnline;
		}

		#region TcpServer Callbacks

		private void Subscribe(IcdTcpServer tcpServer)
		{
			if (tcpServer == null)
				return;

			tcpServer.OnDataReceived += IncomingServerOnDataReceived;
		}

		private void Unsubscribe(IcdTcpServer tcpServer)
		{
			if (tcpServer == null)
				return;

			tcpServer.OnDataReceived -= IncomingServerOnDataReceived;
		}

		/// <summary>
		/// Send received server data to port
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="dataReceiveEventArgs"></param>
		protected abstract void IncomingServerOnDataReceived(object sender, DataReceiveEventArgs dataReceiveEventArgs);

		#endregion

		#region TPort Callbacks

		protected virtual void Subscribe(TPort port)
		{
			if (port == null)
				return;

			port.OnIsOnlineStateChanged += PortOnIsOnlineStateChanged;
		}

		protected virtual void Unsubscribe(TPort port)
		{
			if (port == null)
				return;

			port.OnIsOnlineStateChanged -= PortOnIsOnlineStateChanged;
		}

		private void PortOnIsOnlineStateChanged(object sender, DeviceBaseOnlineStateApiEventArgs e)
		{
			UpdateCachedOnlineStatus();
		}

		#endregion

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(TSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Port = m_Port.Id;
			settings.TcpServerPort = TcpServerPort;
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			Port = null;

			if (m_TcpServer != null)
				m_TcpServer.Stop();

			Unsubscribe(m_TcpServer);
			m_TcpServer = null;
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(TSettings settings, IDeviceFactory factory)
		{

			base.ApplySettingsFinal(settings, factory);

			TPort port = null;

			if (settings.Port != null)
			{
				try
				{
					port = factory.GetPortById((int)settings.Port) as TPort;
				}
				catch (KeyNotFoundException)
				{
					Logger.Log(eSeverity.Error, "No {1} with id {0}", settings.Port, typeof(TPort));
				}
			}

			Port = port;
			UpdateCachedOnlineStatus();

			if (settings.TcpServerPort == null)
			{
				Logger.Log(eSeverity.Error, "TCP Server port not specified in config");
			}
			else
			{
				m_TcpServer = new IcdTcpServer();
				Subscribe(m_TcpServer);
				m_TcpServer.Name = String.Format("{0} TCP Server", settings.Name);
				m_TcpServer.MaxNumberOfClients = 10;
				m_TcpServer.Port = settings.TcpServerPort.Value;

			}
		}

		/// <summary>
		/// Override to add actions on StartSettings
		/// This should be used to start communications with devices and perform initial actions
		/// </summary>
		protected override void StartSettingsFinal()
		{
			base.StartSettingsFinal();

			m_TcpServer.Start();
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("TCP Server Port", TcpServerPort);
		}

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			if (m_TcpServer != null)
				yield return m_TcpServer;
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}
	}
}