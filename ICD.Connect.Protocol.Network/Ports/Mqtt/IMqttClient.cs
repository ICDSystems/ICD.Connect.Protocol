﻿using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Connect.Protocol.Network.EventArguments;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.Network.Ports.Mqtt
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
		/// <returns></returns>
		ushort Subscribe([NotNull] IDictionary<string, byte> topics);

		/// <summary>
		/// Unsubscribe from the given topics.
		/// </summary>
		/// <param name="topics"></param>
		/// <returns></returns>
		ushort Unsubscribe([NotNull] IEnumerable<string> topics);

		/// <summary>
		/// Publish the given topic.
		/// </summary>
		/// <param name="topic"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		ushort Publish(string topic, [NotNull] byte[] message);

		/// <summary>
		/// Publish the given topic.
		/// </summary>
		/// <param name="topic"></param>
		/// <param name="message"></param>
		/// <param name="qosLevel"></param>
		/// <param name="retain"></param>
		/// <returns></returns>
		ushort Publish(string topic, [NotNull] byte[] message, byte qosLevel, bool retain);

		/// <summary>
		/// Clears the retained message with the given topic.
		/// </summary>
		/// <param name="topic"></param>
		/// <returns></returns>
		ushort Clear(string topic);

		#endregion
	}

	public static class MqttClientExtensions
	{
		/// <summary>
		/// Subscribe to the given topic.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="topic"></param>
		/// <param name="qosLevel"></param>
		/// <returns></returns>
		public static ushort Subscribe([NotNull] this IMqttClient extends, string topic, byte qosLevel)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			Dictionary<string, byte> topics =
				new Dictionary<string, byte>
				{
					{topic, qosLevel}
				};

			return extends.Subscribe(topics);
		}
	}
}
