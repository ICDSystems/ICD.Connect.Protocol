using System;
using System.Collections.Generic;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Network.EventArguments;
using ICD.Connect.Protocol.Network.Utils;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings;

namespace ICD.Connect.Protocol.Network.Ports.Mqtt
{
	public abstract class AbstractMqttClient<TSettings> : AbstractConnectablePort<TSettings>, IMqttClient
		where TSettings : IMqttClientSettings, new()
	{
		/// <summary>
		/// Raised when a published message is received.
		/// </summary>
		public abstract event EventHandler<MqttMessageEventArgs> OnMessageReceived;

		private readonly LastWillAndTestament m_Will;

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
		/// Gets/sets the proxy hostname.
		/// </summary>
		public string ProxyHostname { get; set; }

		/// <summary>
		/// Gets/sets the proxy port.
		/// </summary>
		public ushort ProxyPort { get; set; }

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

		/// <summary>
		/// Gets/sets the path to the certificate-authority certificate.
		/// </summary>
		public string CaCertPath { get; set; }

		/// <summary>
		/// Gets the last will and testament parameters.
		/// </summary>
		public LastWillAndTestament Will { get { return m_Will; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractMqttClient()
		{
			m_Will = new LastWillAndTestament();
		}

		#region Methods

		/// <summary>
		/// Subscribe to the given topics.
		/// </summary>
		/// <param name="topics1"></param>
		/// <returns></returns>
		public abstract ushort Subscribe(IDictionary<string, byte> topics1);

		/// <summary>
		/// Unsubscribe from the given topics.
		/// </summary>
		/// <param name="topics"></param>
		/// <returns></returns>
		public abstract ushort Unsubscribe(IEnumerable<string> topics);

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

		/// <summary>
		/// Clears the retained message with the given topic.
		/// </summary>
		/// <param name="topic"></param>
		/// <returns></returns>
		public ushort Clear(string topic)
		{
			return Publish(topic, new byte[0], MqttUtils.QOS_LEVEL_AT_LEAST_ONCE, true);
		}

		/// <summary>
		/// Override to add additional properties to the ToString representation.
		/// </summary>
		/// <param name="addPropertyAndValue"></param>
		protected override void BuildStringRepresentationProperties(Action<string, object> addPropertyAndValue)
		{
			base.BuildStringRepresentationProperties(addPropertyAndValue);

			addPropertyAndValue("Host", new HostInfo(Hostname, Port));
		}

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
			ProxyHostname = null;
			ProxyPort = 0;
			ClientId = null;
			Username = null;
			Password = null;
			Secure = false;
			CaCertPath = null;
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
			settings.ProxyHostname = ProxyHostname;
			settings.ProxyPort = ProxyPort;
			settings.ClientId = ClientId;
			settings.Username = Username;
			settings.Password = Password;
			settings.Secure = Secure;
			settings.CaCertPath = CaCertPath;
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
			ProxyHostname = settings.ProxyHostname;
			ProxyPort = settings.ProxyPort;
			ClientId = settings.ClientId;
			Username = settings.Username;
			Password = settings.Password;
			Secure = settings.Secure;
			CaCertPath = settings.CaCertPath;
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			foreach (IConsoleNodeBase node in MqttClientConsole.GetConsoleNodes(this))
				yield return node;
		}

		/// <summary>
		/// Wrokaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			MqttClientConsole.BuildConsoleStatus(this, addRow);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			foreach (IConsoleCommand command in MqttClientConsole.GetConsoleCommands(this))
				yield return command;
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
