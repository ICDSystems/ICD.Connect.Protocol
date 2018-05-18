﻿using System;
using System.Text;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings;
#if SIMPLSHARP
using Crestron.SimplSharp;
using Crestron.SimplSharp.Ssh;
using Crestron.SimplSharp.Ssh.Common;
#else
using Renci.SshNet;
using Renci.SshNet.Common;
using System.Net.Sockets;
#endif

namespace ICD.Connect.Protocol.Network.Ports.Ssh
{
	/// <summary>
	/// Port for communication with an SSH device.
	/// </summary>
	public sealed class SshPort : AbstractSecureNetworkPort<SshPortSettings>
	{
		public const ushort DEFAULT_PORT = 22;

		private readonly SafeCriticalSection m_SshSection;

		private readonly SecureNetworkProperties m_NetworkProperties;

		private KeyboardInteractiveConnectionInfo m_ConnectionInfo;
		private ShellStream m_SshStream;
		private SshClient m_SshClient;

		#region Properties

		/// <summary>
		/// Gets/sets the password.
		/// </summary>
		[PublicAPI]
		public override string Password
		{
			get { return m_NetworkProperties.NetworkPassword; }
			set { m_NetworkProperties.NetworkPassword = value; }
		}

		/// <summary>
		/// Gets the Secure Network configuration properties.
		/// </summary>
		protected override SecureNetworkProperties SecureNetworkProperties { get { return m_NetworkProperties; } }

		/// <summary>
		/// Gets/sets the username.
		/// </summary>
		[PublicAPI]
		public override string Username
		{
			get { return m_NetworkProperties.NetworkUsername; }
			set { m_NetworkProperties.NetworkUsername = value; }
		}

		/// <summary>
		/// Gets/sets the port.
		/// </summary>
		[PublicAPI]
		public override ushort Port
		{
			get { return m_NetworkProperties.NetworkPort ?? 22; }
			set { m_NetworkProperties.NetworkPort = value == 0 ? (ushort)22 : value; }
		}

		/// <summary>
		/// Gets/sets the address.
		/// </summary>
		[PublicAPI]
		public override string Address { get; set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public SshPort()
		{
			m_SshSection = new SafeCriticalSection();
			m_NetworkProperties = new SecureNetworkProperties();
		}

		/// <summary>
		/// Destructor.
		/// </summary>
		~SshPort()
		{
			Dispose();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Connect via SSH
		/// </summary>
		public override void Connect()
		{
			Disconnect();

			m_SshSection.Enter();

			try
			{
				if (Address == null)
				{
					Log(eSeverity.Error, "Failed to connect - Address is null");
					return;
				}

				if (Username == null)
				{
					Log(eSeverity.Error, "Failed to connect - Username is null");
					return;
				}

				m_ConnectionInfo = new KeyboardInteractiveConnectionInfo(Address, Port, Username);
				Subscribe(m_ConnectionInfo);

				try
				{
					m_SshClient = new SshClient(m_ConnectionInfo)
					{
						// Added keepalive based on Biamp SSH implementation
						KeepAliveInterval = new TimeSpan(0, 0, 30)
					};

					// Added Subscribe here, mainly to catch HostKeyReceived events, to help initial connections
					Subscribe(m_SshClient);
					m_SshClient.Connect();
				}
					// Catches when we attempt to connect to an invalid/offline endpoint.
				catch (SshException e)
				{
					// Potential fix for "Message type 80 is not valid" - the Crestron SSH implementation should be ignoring this.
					if (!e.Message.Contains("Message type 80 is not valid"))
					{
						DisposeClient();
						Log(eSeverity.Error, "Failed to connect - {0}", e.GetBaseException().Message);
					}
				}
					// Catches when we attempt to connect to an invalid/offline endpoint.
				catch (SocketException e)
				{
					DisposeClient();
					Log(eSeverity.Error, "Failed to connect - {0}", e.GetBaseException().Message);
				}

				// ShellStream can only be instantiated when the client is connected.
				if (m_SshClient == null || !m_SshClient.IsConnected)
					return;

				int failCount = 0;

				while (failCount < 5)
				{
					try
					{
						m_SshStream = m_SshClient.CreateShellStream("Crestron SSH Session", 80, 120, 800, 600, 16384);
						Subscribe(m_SshStream);
					}
					catch (SshException e)
					{
						failCount++;
						DisposeStream();

						// Potential fix for "Message type 80 is not valid" - the Crestron SSH implementation should be ignoring this.
						if (e.Message.Contains("Message type 80 is not valid"))
							continue;

						Log(eSeverity.Error, "Failed to create shell stream - {0}", e.Message);
					}

					break;
				}
			}
			finally
			{
				m_SshSection.Leave();

				// Subscribe to the SSH Client outside of critical sections. We only subscribe to events to
				// determine if the port loses connection, in which case a deadlock could happen.
				// DREWS NOTE:  I moved this up into the critical section, because I think we need to respond to HostKeyReceived events.
				//              (Based on looking at Biamp Module SSH implementation)
				//              I'm not sure how this would cause a deadlock?
				// Subscribe(m_SshClient);

				UpdateIsConnectedState();
			}
		}

		/// <summary>
		/// SSH Disconnect
		/// </summary>
		public override void Disconnect()
		{
			m_SshSection.Enter();

			try
			{
				DisposeConnectionInfo();
				DisposeStream();
				DisposeClient();
			}
			finally
			{
				m_SshSection.Leave();
				UpdateIsConnectedState();
			}
		}

		/// <summary>
		/// Dispose the connection info object.
		/// </summary>
		private void DisposeConnectionInfo()
		{
			if (m_ConnectionInfo == null)
				return;

			Unsubscribe(m_ConnectionInfo);
			m_ConnectionInfo.Dispose();

			m_ConnectionInfo = null;

			UpdateIsConnectedState();
		}

		/// <summary>
		/// Dispose the ssh client.
		/// </summary>
		private void DisposeClient()
		{
			if (m_SshClient == null)
				return;

			Unsubscribe(m_SshClient);
			m_SshClient.Disconnect();

			//Disposing the client, let's dispose the stream too?
			DisposeStream();

			//Disposing the client in a different thread, because we've seen it lock up before?
			ThreadingUtils.SafeInvoke(m_SshClient.Dispose);

			m_SshClient = null;

			UpdateIsConnectedState();
		}

		/// <summary>
		/// Dispose the shell stream.
		/// </summary>
		private void DisposeStream()
		{
			if (m_SshStream == null)
				return;

			Unsubscribe(m_SshStream);

#if SIMPLSHARP
			// Sometimes the SSHStream will try to close gracefully when we don't have an active connection.
			// This will raise a base Exception.
			try
			{
				m_SshStream.Close();
			}
			catch (Exception e)
			{
				Log(eSeverity.Warning, "Failed to close SSHStream - {0}", e.Message);
			}
#endif

			try
			{
				m_SshStream.Dispose();
			}
			catch (Exception e)
			{
				Log(eSeverity.Warning, "Failed to dispose SSHStream - {0}", e.Message);
			}

			m_SshStream = null;

			UpdateIsConnectedState();
		}

		/// <summary>
		/// Send command via SSH
		/// </summary>
		/// <param name="data"></param>
		protected override bool SendFinal(string data)
		{
			m_SshSection.Enter();

			try
			{
				if (m_SshStream == null)
				{
					Log(eSeverity.Error, "Unable to write to stream - stream is null");
					return false;
				}

				PrintTx(data);

				m_SshStream.Write(data);
				return true;
			}
			// Thrown when we lose connection.
			catch (SshConnectionException e)
			{
				Log(eSeverity.Error, "Failed writing to stream - {0}", e.Message);
				return false;
			}
			catch (ObjectDisposedException e)
			{
				// ObjectDisposedException message is kinda worthless on its own
				Log(eSeverity.Error, "Failed writing to stream - {0} {1}", e.GetType().Name, e.Message);

				// Stream is broken so clean it up
				DisposeStream();

				return false;
			}
			finally
			{
				m_SshSection.Leave();
				UpdateIsConnectedState();
			}
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(SshPortSettings settings)
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
		protected override void ApplySettingsFinal(SshPortSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			m_NetworkProperties.Copy(settings);
		}

		#endregion

		#region Connection Info Callbacks

		/// <summary>
		/// Subscribe to the connection info events.
		/// </summary>
		/// <param name="connectionInfo"></param>
		private void Subscribe(KeyboardInteractiveConnectionInfo connectionInfo)
		{
			connectionInfo.AuthenticationPrompt += ConnInfoAuthenticationPrompt;
		}

		/// <summary>
		/// Unsubscribes from the connection info.
		/// </summary>
		/// <param name="connectionInfo"></param>
		private void Unsubscribe(KeyboardInteractiveConnectionInfo connectionInfo)
		{
			connectionInfo.AuthenticationPrompt -= ConnInfoAuthenticationPrompt;
		}

		/// <summary>
		/// Handle SSH Authentication prompt
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ConnInfoAuthenticationPrompt(object sender, AuthenticationPromptEventArgs e)
		{
			foreach (AuthenticationPrompt p in e.Prompts)
				p.Response = Password;
		}

		#endregion

		#region SSH Stream Callbacks

		/// <summary>
		/// Subscribe to the stream events.
		/// </summary>
		/// <param name="sshStream"></param>
		private void Subscribe(ShellStream sshStream)
		{
			sshStream.DataReceived += DataReceived;
			sshStream.ErrorOccurred += SshStreamOnErrorOccurred;
		}

		/// <summary>
		/// Unsubscribe from the stream events.
		/// </summary>
		/// <param name="sshStream"></param>
		private void Unsubscribe(ShellStream sshStream)
		{
			sshStream.DataReceived -= DataReceived;
			sshStream.ErrorOccurred -= SshStreamOnErrorOccurred;
		}

		/// <summary>
		/// Handle SSH Data Received
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void DataReceived(object sender, ShellDataEventArgs args)
		{
			string data = Encoding.UTF8.GetString(args.Data, 0, args.Data.Length);

			PrintRx(data);
			Receive(data);
		}

		/// <summary>
		/// Handle SSH Stream Exception.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void SshStreamOnErrorOccurred(object sender, ExceptionEventArgs args)
		{
			Log(eSeverity.Error, args.Exception, "SSH Stream Error - {0}", args.Exception.Message);
			UpdateIsConnectedState();
		}

		#endregion

		#region SSH Client Callbacks

		/// <summary>
		/// Subscribe to the client events.
		/// </summary>
		/// <param name="sshClient"></param>
		private void Subscribe(SshClient sshClient)
		{
			if (sshClient == null)
				return;

			sshClient.ErrorOccurred += SshClientOnErrorOccurred;
			sshClient.HostKeyReceived += SshClientOnHostKeyReceived;
		}

		/// <summary>
		/// Unsubscribe from the client events.
		/// </summary>
		/// <param name="sshClient"></param>
		private void Unsubscribe(SshClient sshClient)
		{
			if (sshClient == null)
				return;

			sshClient.ErrorOccurred -= SshClientOnErrorOccurred;
			sshClient.HostKeyReceived -= SshClientOnHostKeyReceived;
		}

		/// <summary>
		/// Called when the ssh client connects with the server.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="hostKeyEventArgs"></param>
		private void SshClientOnHostKeyReceived(object sender, HostKeyEventArgs hostKeyEventArgs)
		{
			// Added this based on Biamp SSH Transport code
			// This could be what's causing disconnects early in the code
			hostKeyEventArgs.CanTrust = true;

			// Removed this - shouldn't need to update connected state yet.
			// UpdateIsConnectedState();
		}

		/// <summary>
		/// Handle SSH Exception
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void SshClientOnErrorOccurred(object sender, ExceptionEventArgs args)
		{
			Log(eSeverity.Error, "Internal Error - {0}", args.Exception.Message);
			UpdateIsConnectedState();
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets the current connection state of the wrapped SSH client.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsConnectedState()
		{
			return m_SshClient != null && m_SshClient.IsConnected && m_SshStream != null;
		}

		/// <summary>
		/// Override to add additional properties to the ToString representation.
		/// </summary>
		/// <param name="addPropertyAndValue"></param>
		protected override void BuildStringRepresentationProperties(Action<string, object> addPropertyAndValue)
		{
			base.BuildStringRepresentationProperties(addPropertyAndValue);

			addPropertyAndValue("Host", new HostInfo(Address, Port));
		}

		#endregion
	}
}
