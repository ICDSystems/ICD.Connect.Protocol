using System;
using ICD.Connect.Protocol.IoT.EventArguments;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.IoT.Ports
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
