using System;
using System.Collections.Generic;
using ICD.Common.Logging.LoggingContexts;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings;

namespace ICD.Connect.Protocol.Network.Ports.Tcp
{
	public sealed partial class IcdTcpClient : AbstractNetworkPort<IcdTcpClientSettings>
	{
		private const ushort DEFAULT_PORT = 23;
		private const ushort DEFAULT_BUFFER_SIZE = 16384;

		private readonly SafeMutex m_SocketMutex;
		private readonly NetworkProperties m_NetworkProperties;
		private readonly ThreadedWorkerQueue<string> m_SendWorkerQueue;

		#region Properties

		/// <summary>
		/// Gets the Network configuration properties.
		/// </summary>
		public override INetworkProperties NetworkProperties { get { return m_NetworkProperties; } }

		/// <summary>
		/// Get or set the hostname of the remote TCP server.
		/// </summary>
		[PublicAPI]
		public override string Address { get; set; }

		/// <summary>
		/// Get or set the port of the remote TCP server.
		/// </summary>
		[PublicAPI]
		public override ushort Port { get; set; }

		/// <summary>
		/// Get or set the receive buffer size.
		/// </summary>
		[PublicAPI]
		public ushort BufferSize { get; set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		public IcdTcpClient()
		{
			m_NetworkProperties = new NetworkProperties();
			m_SocketMutex = new SafeMutex();

			m_SendWorkerQueue = new ThreadedWorkerQueue<string>(SendWorkerAction);

			Port = DEFAULT_PORT;
			BufferSize = DEFAULT_BUFFER_SIZE;

			IcdEnvironment.OnEthernetEvent += IcdEnvironmentOnEthernetEvent;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			IcdEnvironment.OnEthernetEvent -= IcdEnvironmentOnEthernetEvent;

			m_SendWorkerQueue.Dispose();

			base.DisposeFinal(disposing);
		}

		public void Connect(HostInfo info)
		{
			Address = info.Address;
			Port = info.Port;
			Connect();
		}

		/// <summary>
		/// Disconnects from the remote end point
		/// </summary>
		/// <returns></returns>
		public override void Disconnect()
		{
			if (!m_SocketMutex.WaitForMutex(1000))
			{
				Logger.Log(eSeverity.Error, "Failed to obtain SocketMutex for disconnect");
				return;
			}

			try
			{
				DisposeTcpClient();
			}
			catch (Exception e)
			{
				Logger.Log(eSeverity.Error, e, "Failed to disconnect from host {0}:{1}", Address, Port);
			}
			finally
			{
				m_SocketMutex.ReleaseMutex();
			}
		}

		#endregion

		#region Private Methods


		/// <summary>
		/// Sends a Byte for Byte string (ISO-8859-1)
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		protected override bool SendFinal(string data)
		{
			m_SendWorkerQueue.Enqueue(data);
			// Now that we're doing the worker queue, we don't have real value to return here
			return true;
		}

		/// <summary>
		/// Called when IsConnected state changes
		/// Called before OnlineStatus is updated, and before any events are raised
		/// </summary>
		/// <param name="isConnected"></param>
		protected override void HandleIsConnectedStateChange(bool isConnected)
		{
			base.HandleIsConnectedStateChange(isConnected);

			if (!isConnected)
				m_SendWorkerQueue.Clear();
		}

		/// <summary>
		/// Called when the processor ethernet adapter changes state.
		/// We connect/disconnect to the endpoint accordingly.
		/// </summary>
		/// <param name="adapter"></param>
		/// <param name="type"></param>
		private void IcdEnvironmentOnEthernetEvent(IcdEnvironment.eEthernetAdapterType adapter,
		                                           IcdEnvironment.eEthernetEventType type)
		{
			if (m_TcpClient == null)
				return;

#if SIMPLSHARP
			if (adapter != IcdEnvironment.GetEthernetAdapterType(m_TcpClient.EthernetAdapter))
				return;
#endif
			switch (type)
			{
				case IcdEnvironment.eEthernetEventType.LinkDown:
					Disconnect();
					break;
			}
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			ApplyConfiguration();

			BufferSize = DEFAULT_BUFFER_SIZE;
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(IcdTcpClientSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			ApplyConfiguration();
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<ushort>("SetBufferSize",
			                                               "Sets the buffer size for next connection attempt",
			                                               s => BufferSize = s);
		}

		/// <summary>
		/// Workaround to avoid "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
