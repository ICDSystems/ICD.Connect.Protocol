﻿using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings;

namespace ICD.Connect.Protocol.Network.Ports.Tcp
{
	public sealed partial class AsyncTcpClient : AbstractNetworkPort<AsyncTcpClientSettings>
	{
		private const ushort DEFAULT_BUFFER_SIZE = 16384;

		private readonly SafeMutex m_SocketMutex;
		private readonly NetworkProperties m_NetworkProperties;

		#region Properties

		/// <summary>
		/// Get or set the hostname of the remote TCP server.
		/// </summary>
		[PublicAPI]
		public override string Address
		{
			get { return m_NetworkProperties.NetworkAddress; }
			set { m_NetworkProperties.NetworkAddress = value; }
		}

		/// <summary>
		/// Get or set the port of the remote TCP server.
		/// </summary>
		[PublicAPI]
		public override ushort Port
		{
			get { return m_NetworkProperties.NetworkPort ?? 0; }
			set { m_NetworkProperties.NetworkPort = value; }
		}

		/// <summary>
		/// Gets the Network configuration properties.
		/// </summary>
		protected override INetworkProperties NetworkProperties { get { return m_NetworkProperties; } }

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
		public AsyncTcpClient()
		{
			m_NetworkProperties = new NetworkProperties();
			m_SocketMutex = new SafeMutex();

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
				Log(eSeverity.Error, "Failed to obtain SocketMutex for disconnect");
				return;
			}

			try
			{
				DisposeTcpClient();
			}
			catch (Exception e)
			{
				Log(eSeverity.Error, e, "Failed to disconnect from host {0}:{1}", Address, Port);
			}
			finally
			{
				m_SocketMutex.ReleaseMutex();
			}
		}

		#endregion

		#region Private Methods

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

		/// <summary>
		/// Override to add additional properties to the ToString representation.
		/// </summary>
		/// <param name="addPropertyAndValue"></param>
		protected override void BuildStringRepresentationProperties(Action<string, object> addPropertyAndValue)
		{
			base.BuildStringRepresentationProperties(addPropertyAndValue);

			addPropertyAndValue("Address", Address);
			addPropertyAndValue("Port", Port);
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(AsyncTcpClientSettings settings)
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
		protected override void ApplySettingsFinal(AsyncTcpClientSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			m_NetworkProperties.Copy(settings);
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
