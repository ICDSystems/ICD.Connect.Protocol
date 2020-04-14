using System;
using ICD.Connect.Protocol.IoT.EventArguments;

namespace ICD.Connect.Protocol.IoT.Ports
{
	public sealed class MockMqttClient : AbstractMqttClient<MockMqttClientSettings>
	{
		/// <summary>
		/// Raised when a published message is received.
		/// </summary>
		public override event EventHandler<MqttMessageEventArgs> OnMessageReceived;

		private bool m_IsConnected;

		#region Properties

		/// <summary>
		/// Gets the connection state.
		/// </summary>
		public override bool IsConnected { get { return m_IsConnected; } }

		#endregion

		/// <summary>
		/// Connect to the broker.
		/// </summary>
		/// <returns></returns>
		public override void Connect()
		{
			m_IsConnected = true;
			UpdateCachedOnlineStatus();
		}

		/// <summary>
		/// Disconnect from the broker.
		/// </summary>
		public override void Disconnect()
		{
			m_IsConnected = false;
			UpdateCachedOnlineStatus();
		}

		/// <summary>
		/// Subscribe to the given topics.
		/// </summary>
		/// <param name="topics"></param>
		/// <param name="qosLevels"></param>
		/// <returns></returns>
		public override ushort Subscribe(string[] topics, byte[] qosLevels)
		{
			return 0;
		}

		/// <summary>
		/// Unsubscribe from the given topics.
		/// </summary>
		/// <param name="topics"></param>
		/// <returns></returns>
		public override ushort Unsubscribe(string[] topics)
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
			return 0;
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
			return 0;
		}

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return IsConnected;
		}
	}
}
