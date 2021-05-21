#if !SIMPLSHARP
using System;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Security.Principal;
using ICD.Common.Logging.LoggingContexts;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Network.Ports.NamedPipe.Sockets;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings;

namespace ICD.Connect.Protocol.Network.Ports.NamedPipe
{
	public sealed class NamedPipeClient : AbstractSerialPort<NamedPipeClientSettings>
	{
		private readonly NamedPipeProperties m_NamedPipeProperties;
		private readonly SafeMutex m_SocketMutex;
		private readonly ThreadedWorkerQueue<string> m_SendWorkerQueue;

		[CanBeNull]
		private ClientNamedPipeSocket m_Socket;

		#region Properties

		/// <summary>
		/// Gets the configured NamedPipe properties.
		/// </summary>
		public INamedPipeProperties NamedPipeProperties { get { return m_NamedPipeProperties; } }

		/// <summary>
		/// Gets/sets the configurable remote hostname.
		/// </summary>
		public string Hostname { get; set; }

		/// <summary>
		/// Gets/sets the configurable pipe name.
		/// </summary>
		public string PipeName { get; set; }

		/// <summary>
		/// Gets/sets the configurable pipe direction.
		/// </summary>
		public PipeDirection Direction { get; set; }

		/// <summary>
		/// Gets/sets the configurable pipe options.
		/// </summary>
		public PipeOptions Options { get; set; }

		/// <summary>
		/// Gets/sets the configurable token impersonation level.
		/// </summary>
		public TokenImpersonationLevel TokenImpersonationLevel { get; set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public NamedPipeClient()
		{
			m_NamedPipeProperties = new NamedPipeProperties();
			m_SocketMutex = new SafeMutex();
			m_SendWorkerQueue = new ThreadedWorkerQueue<string>(SendWorkerAction);

			Hostname = ".";
			PipeName = null;
			Direction = PipeDirection.InOut;
			Options = PipeOptions.None;
			TokenImpersonationLevel = TokenImpersonationLevel.Identification;

			IcdEnvironment.OnEthernetEvent += IcdEnvironmentOnEthernetEvent;
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			IcdEnvironment.OnEthernetEvent -= IcdEnvironmentOnEthernetEvent;

			base.DisposeFinal(disposing);
		}

		/// <summary>
		/// Disposes the NamedPipeClientStream.
		/// </summary>
		private void DisposeClient()
		{
			Unsubscribe(m_Socket);

			m_Socket?.Dispose();
			m_Socket = null;

			UpdateIsConnectedState();
		}

		#region Methods

		/// <summary>
		/// Connects to the end point.
		/// </summary>
		public override void Connect()
		{
			Disconnect();

			if (!m_SocketMutex.WaitForMutex(1000))
			{
				Logger.Log(eSeverity.Error, "Failed to obtain SocketMutex for connect");
				return;
			}

			try
			{
				NamedPipeClientStream stream =
					new NamedPipeClientStream(Hostname,
					                          PipeName,
					                          Direction,
					                          Options,
					                          TokenImpersonationLevel);
				m_Socket = new ClientNamedPipeSocket(stream, Hostname, PipeName);
				Subscribe(m_Socket);

				m_Socket.ConnectAsync().Wait();

				if (!m_Socket.IsConnected)
					Logger.Log(eSeverity.Error, "Failed to connect to {0}/{1}", Hostname, PipeName);
			}
			catch (AggregateException ae)
			{
				ae.Handle(x =>
				{
					if (x is SocketException)
					{
						Logger.Log(eSeverity.Error, "Failed to connect to {0}/{1} - {2}", Hostname, PipeName, x.Message);
						return true;
					}

					return false;
				});
			}
			catch (Exception e)
			{
				Logger.Log(eSeverity.Error, "Failed to connect to {0}/{1}", Hostname, PipeName, e.Message);
			}
			finally
			{
				m_SocketMutex.ReleaseMutex();

				UpdateIsConnectedState();
			}
		}

		/// <summary>
		/// Disconnects from the end point.
		/// </summary>
		public override void Disconnect()
		{
			if (!m_SocketMutex.WaitForMutex(1000))
			{
				Logger.Log(eSeverity.Error, "Failed to obtain SocketMutex for disconnect");
				return;
			}

			try
			{
				DisposeClient();
			}
			catch (Exception e)
			{
				Logger.Log(eSeverity.Error, e, "Failed to disconnect from {0}/{1}", Hostname, PipeName);
			}
			finally
			{
				m_SocketMutex.ReleaseMutex();
			}
		}

		/// <summary>
		/// Applies the given device configuration properties to the port.
		/// </summary>
		/// <param name="properties"></param>
		public void ApplyDeviceConfiguration(INamedPipeProperties properties)
		{
			if (properties == null)
				throw new ArgumentNullException("properties");

			// Port supercedes device configuration
			INamedPipeProperties config = NamedPipeProperties.Superimpose(properties);

			ApplyConfiguration(config);
		}

		/// <summary>
		/// Applies the network configuration to the port.
		/// </summary>
		public void ApplyConfiguration()
		{
			ApplyConfiguration(NamedPipeProperties);
		}

		/// <summary>
		/// Applies the given configuration properties to the port.
		/// </summary>
		/// <param name="properties"></param>
		public void ApplyConfiguration(INamedPipeProperties properties)
		{
			if (properties == null)
				throw new ArgumentNullException("properties");

			if (properties.NamedPipeHostname != null)
				Hostname = properties.NamedPipeHostname;

			if (properties.NamedPipeName != null)
				PipeName = properties.NamedPipeName;

			if (properties.NamedPipeDirection.HasValue)
				Direction = properties.NamedPipeDirection.Value;

			if (properties.NamedPipeOptions.HasValue)
				Options = properties.NamedPipeOptions.Value;

			if (properties.NamedPipeTokenImpersonationLevel.HasValue)
				TokenImpersonationLevel = properties.NamedPipeTokenImpersonationLevel.Value;
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Override to add additional properties to the ToString representation.
		/// </summary>
		/// <param name="addPropertyAndValue"></param>
		protected override void BuildStringRepresentationProperties(Action<string, object> addPropertyAndValue)
		{
			base.BuildStringRepresentationProperties(addPropertyAndValue);

			addPropertyAndValue("Hostname", Hostname);
			addPropertyAndValue("PipeName", PipeName);
		}

		/// <summary>
		/// Returns the connection state of the wrapped port
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsConnectedState()
		{
			return m_Socket != null && m_Socket.IsConnected;
		}

		/// <summary>
		/// Sends the data to the remote endpoint.
		/// </summary>
		protected override bool SendFinal(string data)
		{
			m_SendWorkerQueue.Enqueue(data);
			// Now that we're doing the worker queue, we don't have real value to return here
			return true;
		}

		/// <summary>
		/// Sends a Byte for Byte string (ISO-8859-1)
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		private void SendWorkerAction(string data)
		{
			byte[] bytes = StringUtils.ToBytes(data);
			try
			{
				PrintTx(data);
				m_Socket.SendAsync(bytes);
			}
			catch (SocketException e)
			{
				Logger.Log(eSeverity.Error, "Failed to send data - {0}", e.Message);
			}
			finally
			{
				UpdateIsConnectedState();
			}
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
			switch (type)
			{
				case IcdEnvironment.eEthernetEventType.LinkDown:
					Disconnect();
					break;
			}
		}

		#endregion

		#region Socket Callbacks

		/// <summary>
		/// Subscribe to the socket events.
		/// </summary>
		/// <param name="socket"></param>
		private void Subscribe([CanBeNull] ClientNamedPipeSocket socket)
		{
			if (socket == null)
				return;

			socket.OnDataReceived += SocketOnDataReceived;
			socket.OnIsConnectedChanged += SocketOnIsConnectedChanged;
		}

		/// <summary>
		/// Unsubscribe from the socket events.
		/// </summary>
		/// <param name="socket"></param>
		private void Unsubscribe([CanBeNull] ClientNamedPipeSocket socket)
		{
			if (socket == null)
				return;

			socket.OnDataReceived -= SocketOnDataReceived;
			socket.OnIsConnectedChanged -= SocketOnIsConnectedChanged;
		}

		/// <summary>
		/// Called when the socket connects/disconnects.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void SocketOnIsConnectedChanged(object sender, BoolEventArgs e)
		{
			UpdateIsConnectedState();
		}

		/// <summary>
		/// Called when data is received from the remote endpoint.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void SocketOnDataReceived(object sender, GenericEventArgs<byte[]> eventArgs)
		{
			string data = StringUtils.ToString(eventArgs.Data);

			PrintRx(data);
			Receive(data);

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

			NamedPipeProperties.ClearNamedPipeProperties();
			ApplyConfiguration();
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(NamedPipeClientSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Copy(NamedPipeProperties);
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(NamedPipeClientSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			NamedPipeProperties.Copy(settings);
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

			addRow("Hostname", Hostname);
			addRow("Pipe Name", PipeName);
			addRow("Direction", Direction);
			addRow("Options", Options);
			addRow("Token Impersonation Level", TokenImpersonationLevel);
		}

		#endregion
	}
}
#endif
