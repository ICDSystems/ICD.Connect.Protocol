using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.IoT.EventArguments;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace ICD.Connect.Protocol.IoT.Ports
{
	public sealed class IcdMqttClient : AbstractPort<IcdMqttClientSettings>
	{
		/// <summary>
		/// Raised when a published message is received.
		/// </summary>
		public event EventHandler<MqttMessageEventArgs> OnMessageReceived;

		private MqttClient m_Client;

		#region Properties

		/// <summary>
		/// Gets/sets the wrapped client.
		/// </summary>
		private MqttClient Client
		{
			get { return m_Client; }
			set
			{
				if (value == m_Client)
					return;

				Unsubscribe(m_Client);
				m_Client = value;
				Subscribe(m_Client);
			}
		}

		public bool IsConnected { get { return m_Client != null && m_Client.IsConnected; } }

		/// <summary>
		/// Gets/sets the hostname.
		/// </summary>
		public string Hostname { get; set; }

		public int Port { get; set; }

		/// <summary>
		/// Gets/sets the client id.
		/// </summary>
		public string ClientId { get; set; }

		/// <summary>
		/// Gets/sets the username.
		/// </summary>
		public string Username { get; set; }

		/// <summary>
		/// Gets/sets the password.
		/// </summary>
		public string Password { get; set; }

		public bool Secure { get; set; }

		#endregion

		/// <summary>
		/// Release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnMessageReceived = null;

			base.DisposeFinal(disposing);
		}

		#region Methods

		/// <summary>
		/// Connect to the broker.
		/// </summary>
		/// <returns></returns>
		public byte Connect()
		{
			Disconnect();
			if (Secure)
				Client = new MqttClient(Hostname, Port, true, null, null, MqttSslProtocols.TLSv1_2);
			else
				Client = new MqttClient(Hostname, Port, false, null, null, MqttSslProtocols.None);
			return Client.Connect(ClientId, Username, Password);
		}

		/// <summary>
		/// Disconnect from the broker.
		/// </summary>
		public void Disconnect()
		{
			if (m_Client != null)
				m_Client.Disconnect();

			Client = null;
		}

		public ushort Subscribe(string[] topics, byte[] qosLevels)
		{
			if (m_Client == null)
				throw new InvalidOperationException("No client connected.");

			return m_Client.Subscribe(topics, qosLevels);
		}

		public ushort Unsubscribe(string[] topics)
		{
			if (m_Client == null)
				throw new InvalidOperationException("No client connected.");

			return m_Client.Unsubscribe(topics);
		}

		public ushort Publish(string topic, byte[] message)
		{
			if (m_Client == null)
				throw new InvalidOperationException("No client connected.");

			return m_Client.Publish(topic, message);
		}

		public ushort Publish(string topic, byte[] message, byte qosLevel, bool retain)
		{
			if (m_Client == null)
				throw new InvalidOperationException("No client connected.");

			return m_Client.Publish(topic, message, qosLevel, retain);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return m_Client != null && m_Client.IsConnected;
		}

		#endregion

		#region Client Callbacks

		/// <summary>
		/// Subscribe to the client events.
		/// </summary>
		/// <param name="client"></param>
		private void Subscribe([CanBeNull] MqttClient client)
		{
			if (client == null)
				return;

			client.ConnectionClosed += ClientOnConnectionClosed;
			client.MqttMsgPublishReceived += ClientOnMqttMsgPublishReceived;
		}

		/// <summary>
		/// Unsubscribe from the client events.
		/// </summary>
		/// <param name="client"></param>
		private void Unsubscribe([CanBeNull] MqttClient client)
		{
			if (client == null)
				return;

			client.ConnectionClosed -= ClientOnConnectionClosed;
			client.MqttMsgPublishReceived -= ClientOnMqttMsgPublishReceived;
		}

		/// <summary>
		/// Called when the MQTT client receives a published message.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void ClientOnMqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs eventArgs)
		{
			PrintRx(eventArgs.Topic, StringUtils.ToString(eventArgs.Message));

			OnMessageReceived.Raise(this, new MqttMessageEventArgs(eventArgs.Topic, eventArgs.Message));
		}

		/// <summary>
		/// Called when the MQTT client closes the connection.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void ClientOnConnectionClosed(object sender, EventArgs eventArgs)
		{
			UpdateCachedOnlineStatus();
		}

		#endregion

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			Hostname = null;
			ClientId = null;
			Username = null;
			Password = null;
			Secure = false;
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(IcdMqttClientSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Hostname = Hostname;
			settings.ClientId = ClientId;
			settings.Username = Username;
			settings.Password = Password;
			settings.Secure = Secure;
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(IcdMqttClientSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			Hostname = settings.Hostname;
			ClientId = settings.ClientId;
			Username = settings.Username;
			Password = settings.Password;
			Secure = settings.Secure;
		}

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Is Connected", IsConnected);
			addRow("Hostname", Hostname);
			addRow("ClientId", ClientId);
			addRow("Username", Username);
			addRow("Password", Password);
			addRow("Secure", Secure);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new ConsoleCommand("Connect", "Connects to the broker at the configured hostname", () => Connect());
			yield return new ConsoleCommand("Disconnect", "Disconnects from the broker", () => Disconnect());
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}