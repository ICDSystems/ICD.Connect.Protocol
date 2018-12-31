using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Protocol.Network.Udp
{
	public sealed partial class AsyncUdpClient : AbstractSerialPort<AsyncUdpClientSettings>
	{
		public const ushort DEFAULT_BUFFER_SIZE = 16384;
		public const string ACCEPT_ALL = "0.0.0.0";

		private bool m_ListeningRequested;
		private string m_Address;

		#region Properties

		/// <summary>
		/// Address to accept connections from.
		/// </summary>
		[PublicAPI]
		public string Address
		{
			get { return m_Address; }
			set { m_Address = IcdEnvironment.NetworkAddresses.Contains(value) ? "127.0.0.1" : value; }
		}

		/// <summary>
		/// Port to accept connections from.
		/// </summary>
		[PublicAPI]
		public ushort Port { get; set; }

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
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(AsyncUdpClientSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Address = Address;
			settings.Port = Port;
			settings.BufferSize = BufferSize;
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			Address = ACCEPT_ALL;
			Port = 0;
			BufferSize = DEFAULT_BUFFER_SIZE;
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(AsyncUdpClientSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			Address = settings.Address;
			Port = settings.Port;
			BufferSize = settings.BufferSize;
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

			addRow("Address", Address);
			addRow("Port", Port);
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

			yield return new GenericConsoleCommand<string>("SetAddress",
			                                               "Sets the address for next connection attempt",
			                                               s => Address = s);
			yield return new GenericConsoleCommand<ushort>("SetPort",
			                                               "Sets the port for next connection attempt",
			                                               s => Port = s);
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
