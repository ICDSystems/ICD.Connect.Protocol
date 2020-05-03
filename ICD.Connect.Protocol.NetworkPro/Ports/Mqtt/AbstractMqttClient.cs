using System;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.NetworkPro.EventArguments;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings;

namespace ICD.Connect.Protocol.NetworkPro.Ports.Mqtt
{
	public abstract class AbstractMqttClient<TSettings> : AbstractConnectablePort<TSettings>, IMqttClient
		where TSettings : IMqttClientSettings, new()
	{
		/// <summary>
		/// Raised when a published message is received.
		/// </summary>
		public abstract event EventHandler<MqttMessageEventArgs> OnMessageReceived;

		#region Properties

		/// <summary>
		/// Gets/sets the hostname.
		/// </summary>
		public string Hostname { get; set; }

		/// <summary>
		/// Gets/sets the network port.
		/// </summary>
		public ushort Port { get; set; }

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

		/// <summary>
		/// Gets/sets the secure mode.
		/// </summary>
		public bool Secure { get; set; }

		#endregion

		#region Methods

		/// <summary>
		/// Subscribe to the given topics.
		/// </summary>
		/// <param name="topics"></param>
		/// <param name="qosLevels"></param>
		/// <returns></returns>
		public abstract ushort Subscribe(string[] topics, byte[] qosLevels);

		/// <summary>
		/// Unsubscribe from the given topics.
		/// </summary>
		/// <param name="topics"></param>
		/// <returns></returns>
		public abstract ushort Unsubscribe(string[] topics);

		/// <summary>
		/// Publish the given topic.
		/// </summary>
		/// <param name="topic"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		public abstract ushort Publish(string topic, byte[] message);

		/// <summary>
		/// Publish the given topic.
		/// </summary>
		/// <param name="topic"></param>
		/// <param name="message"></param>
		/// <param name="qosLevel"></param>
		/// <param name="retain"></param>
		/// <returns></returns>
		public abstract ushort Publish(string topic, byte[] message, byte qosLevel, bool retain);

		#endregion

		#region Settings

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			Hostname = null;
			Port = 1883; // 1883 default network port for mqtt
			ClientId = null;
			Username = null;
			Password = null;
			Secure = false;
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(TSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Hostname = Hostname;
			settings.Port = Port;
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
		protected override void ApplySettingsFinal(TSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			Hostname = settings.Hostname;
			Port = settings.Port;
			ClientId = settings.ClientId;
			Username = settings.Username;
			Password = settings.Password;
			Secure = settings.Secure;
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
			addRow("Port", Port);
			addRow("ClientId", ClientId);
			addRow("Username", Username);
			addRow("Password", Password);
			addRow("Secure", Secure);
		}

		#endregion
	}
}
