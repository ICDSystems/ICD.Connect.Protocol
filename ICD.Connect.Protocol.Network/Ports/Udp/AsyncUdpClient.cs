using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Settings;

namespace ICD.Connect.Protocol.Network.Ports.Udp
{
	public sealed partial class AsyncUdpClient : AbstractNetworkPort<AsyncUdpClientSettings>
	{
		public const ushort DEFAULT_BUFFER_SIZE = 16384;
		public const string ACCEPT_ALL = "0.0.0.0";

		private readonly NetworkProperties m_NetworkProperties;

		private bool m_ListeningRequested;

		#region Properties

		/// <summary>
		/// Gets the Network configuration properties.
		/// </summary>
		public override INetworkProperties NetworkProperties { get { return m_NetworkProperties; } }

		/// <summary>
		/// Address to accept connections from.
		/// </summary>
		[PublicAPI]
		public override string Address { get; set; }

		/// <summary>
		/// Port to accept connections from.
		/// </summary>
		[PublicAPI]
		public override ushort Port { get; set; }

		/// <summary>
		/// Get or set the receive buffer size.
		/// </summary>
		[PublicAPI]
		public int BufferSize { get; set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public AsyncUdpClient()
		{
			m_NetworkProperties = new NetworkProperties();

			BufferSize = DEFAULT_BUFFER_SIZE;
			Address = ACCEPT_ALL;

			IcdEnvironment.OnEthernetEvent += IcdEnvironmentOnEthernetEvent;
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			IcdEnvironment.OnEthernetEvent -= IcdEnvironmentOnEthernetEvent;

			base.DisposeFinal(disposing);

			Disconnect();
		}

		/// <summary>
		/// Sends serial data to a specific endpoint.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="ipAddress"></param>
		/// <param name="port"></param>
		/// <returns></returns>
		[PublicAPI]
		public bool SendToAddress(string data, string ipAddress, int port)
		{
			try
			{
				if (IsConnected)
					return SendToAddressFinal(data, ipAddress, port);

				Log(eSeverity.Error, "Unable to send to address - Port is not connected.");
				return false;
			}
			finally
			{
				UpdateIsConnectedState();
			}
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Handle Ethernet events
		/// </summary>
		/// <param name="adapter"></param>
		/// <param name="type"></param>
		private void IcdEnvironmentOnEthernetEvent(IcdEnvironment.eEthernetAdapterType adapter,
		                                           IcdEnvironment.eEthernetEventType type)
		{
#if SIMPLSHARP
			IcdEnvironment.eEthernetAdapterType adapterType =
				m_UdpClient == null
					? IcdEnvironment.eEthernetAdapterType.EthernetUnknownAdapter
					: IcdEnvironment.GetEthernetAdapterType(m_UdpClient.EthernetAdapterToBindTo);

			if (adapter != adapterType && adapterType != IcdEnvironment.eEthernetAdapterType.EthernetUnknownAdapter)
				return;
#endif
			switch (type)
			{
				case IcdEnvironment.eEthernetEventType.LinkUp:
					if (m_ListeningRequested)
						Connect();
					break;

				case IcdEnvironment.eEthernetEventType.LinkDown:
					if (m_UdpClient == null)
						break;
#if SIMPLSHARP
					m_UdpClient.DisableUDPServer();
#else
                    m_UdpClient.Dispose();
#endif
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			UpdateIsConnectedState();
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
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(AsyncUdpClientSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			ApplyConfiguration();
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

			addRow("Buffer Size", BufferSize);
#if SIMPLSHARP
			addRow("Server Status", m_UdpClient == null ? string.Empty : m_UdpClient.ServerStatus.ToString());
#endif
		}

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
