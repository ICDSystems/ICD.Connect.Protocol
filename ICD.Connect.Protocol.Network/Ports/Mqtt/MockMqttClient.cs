using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Connect.API.Commands;
using ICD.Connect.Protocol.Network.EventArguments;
using ICD.Connect.Protocol.Network.Utils;

namespace ICD.Connect.Protocol.Network.Ports.Mqtt
{
	public sealed class MockMqttClient : AbstractMqttClient<MockMqttClientSettings>
	{
		/// <summary>
		/// Raised when a published message is received.
		/// </summary>
		public override event EventHandler<MqttMessageEventArgs> OnMessageReceived;

		private readonly Dictionary<string, string> m_TopicsToMessages;
		private bool m_IsConnected;

		public MockMqttClient()
		{
			m_TopicsToMessages = new Dictionary<string, string>();
		}

		/// <summary>
		/// Connect to the broker.
		/// </summary>
		/// <returns></returns>
		public override void Connect()
		{
			m_IsConnected = true;
			UpdateIsConnectedState();
		}

		/// <summary>
		/// Disconnect from the broker.
		/// </summary>
		public override void Disconnect()
		{
			m_IsConnected = false;
			UpdateIsConnectedState();
		}

		/// <summary>
		/// Subscribe to the given topics.
		/// </summary>
		/// <param name="topics"></param>
		/// <returns></returns>
		public override ushort Subscribe(IDictionary<string, byte> topics)
		{
			return 0;
		}

		/// <summary>
		/// Unsubscribe from the given topics.
		/// </summary>
		/// <param name="topics"></param>
		/// <returns></returns>
		public override ushort Unsubscribe(IEnumerable<string> topics)
		{
			return 0;
		}

		/// <summary>
		/// Publish the given topic.
		/// </summary>
		/// <param name="topic"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		public override ushort Publish(string topic, byte[] message)
		{
			return Publish(topic, message, MqttUtils.QOS_LEVEL_AT_MOST_ONCE, false);
		}

		/// <summary>
		/// Publish the given topic.
		/// </summary>
		/// <param name="topic"></param>
		/// <param name="message"></param>
		/// <param name="qosLevel"></param>
		/// <param name="retain"></param>
		/// <returns></returns>
		public override ushort Publish(string topic, byte[] message, byte qosLevel, bool retain)
		{
			AddToCache(topic, message);
			return 0;
		}

		/// <summary>
		/// Returns the connection state of the wrapped port
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsConnectedState()
		{
			return m_IsConnected;
		}

		private void AddToCache(string topic, byte[] message)
		{
			m_TopicsToMessages[topic] = System.Text.Encoding.UTF8.GetString(message, 0, message.Length);
		}

		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
			{
				yield return command;
			}

			yield return new ConsoleCommand("ViewTopicsToMessages", "Prints a dictionary of topics and their messages", () => PrintTopicsToMessages());
		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		private string PrintTopicsToMessages()
		{
			TableBuilder builder = new TableBuilder("Topic", "Messages");
			foreach (KeyValuePair<string, string> topicsToMessage in m_TopicsToMessages)
			{
				builder.AddRow(topicsToMessage.Key, topicsToMessage.Value);
			}

			return builder.ToString();
		}
	}
}
