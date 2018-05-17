using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
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
		/// Address to accept connections from.
		/// </summary>
		[PublicAPI]
		public override string Address
		{
			get { return m_NetworkProperties.NetworkAddress; }
			set { m_NetworkProperties.NetworkAddress = value; }
		}

		/// <summary>
		/// Port to accept connections from.
		/// </summary>
		[PublicAPI]
		public override ushort Port
		{
			get { return m_NetworkProperties.NetworkPort ?? 0; }
			set { m_NetworkProperties.NetworkPort = value; }
		}


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
			return ConnectAndSend(data, s => SendToAddressFinal(s, ipAddress, port));
		}

		#endregion

		#region Private Methods

		protected override void BuildStringRepresentationProperties(Action<string, object> addPropertyAndValue)
		{
			base.BuildStringRepresentationProperties(addPropertyAndValue);

			addPropertyAndValue("Address", Address);
			addPropertyAndValue("Port", Port);
		}

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
				IcdEnvironment.GetEthernetAdapterType(m_UdpClient.EthernetAdapterToBindTo);

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
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(AsyncUdpClientSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Copy(m_NetworkProperties);
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			m_NetworkProperties.Clear();
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(AsyncUdpClientSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			m_NetworkProperties.Copy(settings);
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
