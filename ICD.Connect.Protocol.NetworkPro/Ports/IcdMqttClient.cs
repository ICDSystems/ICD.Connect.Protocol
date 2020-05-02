using System;
using System.Text;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Protocol.NetworkPro.EventArguments;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Exceptions;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace ICD.Connect.Protocol.NetworkPro.Ports
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
				if (Client != null && Client.IsConnected)
					Disconnect();

				if (string.IsNullOrEmpty(Username))
					GetClient().Connect(ClientId);
				else
					GetClient().Connect(ClientId, Username, Password);
			}
			catch (MqttConnectionException e)
			{
				string message = string.Format("Error connecting to {0} - {1}", Hostname, e.Message);
				if (e.InnerException != null)
					message = string.Format("{0} - {1}", message, e.InnerException.Message);

				Logger.Log(eSeverity.Error, message);
			}
			finally
			{
				UpdateCachedOnlineStatus();
				UpdateIsConnectedState();
			}
		}

		/// <summary>
		/// Disconnect from the broker.
		/// </summary>
		public override void Disconnect()
		{
			if (Client != null)
				Client.Disconnect();

			Client = null;

			UpdateCachedOnlineStatus();
			UpdateIsConnectedState();
		}

		/// <summary>
		/// Subscribe to the given topics.
		/// </summary>
		/// <param name="topics"></param>
		/// <param name="qosLevels"></param>
		/// <returns></returns>
		public override ushort Subscribe(string[] topics, byte[] qosLevels)
		{
			string debug =
				new StringBuilder()
					.AppendFormat("Topics: {0}, ", StringUtils.ArrayFormat(topics))
					.AppendFormat("QOS Levels: {0}", StringUtils.ArrayFormat(qosLevels))
					.ToString();

			PrintTx("Subscribe", debug);

			return GetClient().Subscribe(topics, qosLevels);
		}

		/// <summary>
		/// Unsubscribe from the given topics.
		/// </summary>
		/// <param name="topics"></param>
		/// <returns></returns>
		public override ushort Unsubscribe(string[] topics)
		{
			string debug =
				new StringBuilder()
					.AppendFormat("Topics: {0}", StringUtils.ArrayFormat(topics))
					.ToString();

			PrintTx("Unsubscribe", debug);

			return GetClient().Unsubscribe(topics);
		}

		/// <summary>
		/// Publish the given topic.
		/// </summary>
		/// <param name="topic"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		public override ushort Publish(string topic, byte[] message)
		{
			string debug =
				new StringBuilder()
					.AppendFormat("Topic: {0}, ", topic)
					.AppendFormat("Message: {0}", Encoding.UTF8.GetString(message))
					.ToString();

			PrintTx("Publish", debug);

			return GetClient().Publish(topic, message);
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
			string debug =
				new StringBuilder()
					.AppendFormat("Topic: {0}, ", topic)
					.AppendFormat("QOS Level: {0}, ", qosLevel)
					.AppendFormat("Retain: {0}, ", retain)
					.AppendFormat("Message: {0}", Encoding.UTF8.GetString(message))
					.ToString();

			PrintTx("Publish", debug);

			return GetClient().Publish(topic, message, qosLevel, retain);
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

		/// <summary>
		/// Lazy-loads the existing client or a disconnected client.
		/// </summary>
		/// <returns></returns>
		[NotNull]
		private MqttClient GetClient()
		{
			if (Client != null)
				return Client;

			Client = Secure
				? new MqttClient(Hostname, Port, true, null, null, MqttSslProtocols.TLSv1_2)
				: new MqttClient(Hostname, Port, false, null, null, MqttSslProtocols.None);

			return Client;
		}

		/// <summary>
		/// Returns the connection state of the wrapped port
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsConnectedState()
		{
			return GetIsOnlineStatus();
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
			string debug =
				new StringBuilder()
					.AppendFormat("Topic: {0}, ", eventArgs.Topic)
					.AppendFormat("QOS Level: {0}, ", eventArgs.QosLevel)
					.AppendFormat("Retain: {0}, ", eventArgs.Retain)
					.AppendFormat("Message: {0}, ", Encoding.UTF8.GetString(eventArgs.Message))
					.AppendFormat("Duplicate Flag: {0}", eventArgs.DupFlag)
					.ToString();

			PrintRx("Publish Received", debug);

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
			UpdateIsConnectedState();
		}

		#endregion
	}
}