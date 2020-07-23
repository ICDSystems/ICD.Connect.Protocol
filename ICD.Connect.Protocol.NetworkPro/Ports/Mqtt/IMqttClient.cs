using System;
using ICD.Common.Properties;
using ICD.Connect.Protocol.NetworkPro.EventArguments;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.NetworkPro.Ports.Mqtt
{
	public interface IMqttClient : IConnectablePort
	{
		/// <summary>
		/// Raised when a published message is received.
		/// </summary>
		event EventHandler<MqttMessageEventArgs> OnMessageReceived;

		#region Properties

		/// <summary>
		/// Gets/sets the hostname.
		/// </summary>
		string Hostname { get; set; }

		/// <summary>
		/// Gets/sets the network port.
		/// </summary>
		ushort Port { get; set; }

		/// <summary>
		/// Gets/sets the proxy hostname.
		/// </summary>
		string ProxyHostname { get; set; }

		/// <summary>
		/// Gets/sets the proxy port.
		/// </summary>
		ushort ProxyPort { get; set; }

		/// <summary>
		/// Gets/sets the client id.
		/// </summary>
		string ClientId { get; set; }

		/// <summary>
		/// Gets/sets the username.
		/// </summary>
		string Username { get; set; }

		/// <summary>
		/// Gets/sets the password.
		/// </summary>
		string Password { get; set; }

		/// <summary>
		/// Gets/sets the secure mode.
		/// </summary>
		bool Secure { get; set; }

		/// <summary>
		/// Gets/sets the path to the certificate-authority certificate.
		/// </summary>
		string CaCertPath { get; set; }

		/// <summary>
		/// Gets/sets the last will and testament parameters.
		/// </summary>
		[NotNull]
		LastWillAndTestament Will { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Subscribe to the given topics.
		/// </summary>
		/// <param name="topics"></param>
		/// <param name="qosLevels"></param>
		/// <returns></returns>
		ushort Subscribe(string[] topics, byte[] qosLevels);

		/// <summary>
		/// Unsubscribe from the given topics.
		/// </summary>
		/// <param name="topics"></param>
		/// <returns></returns>
		ushort Unsubscribe(string[] topics);

		/// <summary>
		/// Publish the given topic.
		/// </summary>
		/// <param name="topic"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		ushort Publish(string topic, byte[] message);

		/// <summary>
		/// Publish the given topic.
		/// </summary>
		/// <param name="topic"></param>
		/// <param name="message"></param>
		/// <param name="qosLevel"></param>
		/// <param name="retain"></param>
		/// <returns></returns>
		ushort Publish(string topic, byte[] message, byte qosLevel, bool retain);

		#endregion
	}
}
