using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Protocol.Network.Tcp
{
	public sealed partial class AsyncTcpClient : AbstractSerialPort<AsyncTcpClientSettings>
	{
		public const ushort DEFAULT_BUFFER_SIZE = 16384;

		private readonly SafeMutex m_SocketMutex;

		private string m_Address;

		#region Properties

		/// <summary>
		/// Get or set the hostname of the remote TCP server.
		/// </summary>
		[PublicAPI]
		public string Address
		{
			get { return m_Address; }
			set { m_Address = IcdEnvironment.NetworkAddresses.Contains(value) ? "127.0.0.1" : value; }
		}

		/// <summary>
		/// Get or set the port of the remote TCP server.
		/// </summary>
		[PublicAPI]
		public ushort Port { get; set; }

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
				Logger.AddEntry(eSeverity.Error, "{0} failed to obtain SocketMutex for disconnect", this);
				return;
			}

			try
			{
				DisposeTcpClient();
			}
			catch (Exception e)
			{
				Logger.AddEntry(eSeverity.Error, e, "{0} failed to disconnect from host {1}:{2}", this,
				                Address, Port);
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

			Address = null;
			Port = 0;
			BufferSize = DEFAULT_BUFFER_SIZE;
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(AsyncTcpClientSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			Address = settings.Address;
			Port = settings.Port;
			BufferSize = settings.BufferSize;
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
