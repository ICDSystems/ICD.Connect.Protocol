using System;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Protocol.IoT.EventArguments;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Exceptions;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace ICD.Connect.Protocol.IoT.Ports
{
	public sealed class IcdMqttClient : AbstractMqttClient<IcdMqttClientSettings>
	{
		/// <summary>
		/// Raised when a published message is received.
		/// </summary>
		public override event EventHandler<MqttMessageEventArgs> OnMessageReceived;

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

				UpdateCachedOnlineStatus();
			}
		}

		/// <summary>
		/// Gets the connection state.
		/// </summary>
		public override bool IsConnected { get { return m_Client != null && m_Client.IsConnected; } }

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
		public override void Connect()
		{
			try
			{
				if (IsConnected)
					Disconnect();

				Client = Secure
					? new MqttClient(Hostname, Port, true, null, null, MqttSslProtocols.TLSv1_2)
					: new MqttClient(Hostname, Port, false, null, null, MqttSslProtocols.None);

				if (string.IsNullOrEmpty(Username))
					Client.Connect(ClientId);
				else
					Client.Connect(ClientId, Username, Password);
			}
			catch (MqttConnectionException e)
			{
				if (e.InnerException == null)
					throw;

				Logger.Log(eSeverity.Error,
				           string.Format("Error connecting to MQTT Broker.{0}Inner exception is {1}{0}{2}{0}Exception Is{3}{0}{4}",
				                         IcdEnvironment.NewLine,
				                         e.InnerException.Message,
				                         e.InnerException.StackTrace,
				                         e.Message,
				                         e.InnerException));
			}
			finally
			{
				UpdateCachedOnlineStatus();
			}
		}

		/// <summary>
		/// Disconnect from the broker.
		/// </summary>
		public override void Disconnect()
		{
			if (m_Client != null)
				m_Client.Disconnect();

			Client = null;
		}

		/// <summary>
		/// Subscribe to the given topics.
		/// </summary>
		/// <param name="topics"></param>
		/// <param name="qosLevels"></param>
		/// <returns></returns>
		public override ushort Subscribe(string[] topics, byte[] qosLevels)
		{
			if (m_Client == null)
				throw new InvalidOperationException("No client connected.");

			return m_Client.Subscribe(topics, qosLevels);
		}

		/// <summary>
		/// Unsubscribe from the given topics.
		/// </summary>
		/// <param name="topics"></param>
		/// <returns></returns>
		public override ushort Unsubscribe(string[] topics)
		{
			if (m_Client == null)
				throw new InvalidOperationException("No client connected.");

			return m_Client.Unsubscribe(topics);
		}

		/// <summary>
		/// Publish the given topic.
		/// </summary>
		/// <param name="topic"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		public override ushort Publish(string topic, byte[] message)
		{
			if (m_Client == null)
				throw new InvalidOperationException("No client connected.");

			return m_Client.Publish(topic, message);
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
	}
}