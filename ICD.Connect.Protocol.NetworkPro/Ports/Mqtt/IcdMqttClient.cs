using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.IO;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Protocol.NetworkPro.EventArguments;
using ICD.Connect.Protocol.Ports;
#if SIMPLSHARP
using SSMono.Net.Security;
using SSMono.Security.Cryptography.X509Certificates;
#else
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
#endif
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace ICD.Connect.Protocol.NetworkPro.Ports.Mqtt
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

				string debug =
					new StringBuilder()
						.AppendFormat("Topic: {0}, ", Will.Topic)
						.AppendFormat("Message: {0}", Will.Message)
						.ToString();

				PrintTx("Will", debug);

				GetClient().Connect(ClientId,
				                    Username,
				                    Password,
				                    Will.Retain,
				                    Will.QosLevel,
				                    Will.Flag,
				                    Will.Topic,
				                    Will.Message,
				                    true,
				                    60);
			}
			catch (Exception e)
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
			try
			{
				if (Client != null)
					Client.Disconnect();
			}
			catch (Exception e)
			{
				Logger.Log(eSeverity.Error, "Exception on Disconnect - {0}", e.Message);
			}

			Client = null;

			UpdateCachedOnlineStatus();
			UpdateIsConnectedState();
		}

		/// <summary>
		/// Subscribe to the given topics.
		/// </summary>
		/// <param name="topics"></param>
		/// <returns></returns>
		public override ushort Subscribe([NotNull] IDictionary<string, byte> topics)
		{
			if (topics == null)
				throw new ArgumentNullException("topics");

			if (!IsConnected)
			{
				Logger.Log(eSeverity.Error, "Unable to Subscribe - Client is not connected.");
				return 0;
			}

			if (topics.Count == 0)
				return 0;

			string debug =
				new StringBuilder()
					.AppendFormat("Topics: {0}", StringUtils.ArrayFormat(topics.Select(kvp => string.Format("{0} ({1})", kvp.Key, kvp.Value))))
					.ToString();

			PrintTx("Subscribe", debug);

			string[] topicStrings = new string[topics.Count];
			byte[] qosLevels = new byte[topics.Count];

			int index = 0;
			foreach (var kvp in topics)
			{
				topicStrings[index] = kvp.Key;
				qosLevels[index] = kvp.Value;
				index++;
			}

			try
			{
				return GetClient().Subscribe(topicStrings, qosLevels);
			}
			catch (Exception e)
			{
				Logger.Log(eSeverity.Error, "Failed to Subscribe - {0}", e.Message);
				return 0;
			}
		}

		/// <summary>
		/// Unsubscribe from the given topics.
		/// </summary>
		/// <param name="topics"></param>
		/// <returns></returns>
		public override ushort Unsubscribe([NotNull] IEnumerable<string> topics)
		{
			if (topics == null)
				throw new ArgumentNullException("topics");

			if (!IsConnected)
			{
				Logger.Log(eSeverity.Error, "Unable to Unsubscribe - Client is not connected.");
				return 0;
			}

			string[] topicsArray = topics as string[] ?? topics.ToArray();
			if (topicsArray.Length == 0)
				return 0;

			string debug =
				new StringBuilder()
					.AppendFormat("Topics: {0}", StringUtils.ArrayFormat(topicsArray))
					.ToString();

			PrintTx("Unsubscribe", debug);

			try
			{
				return GetClient().Unsubscribe(topicsArray);
			}
			catch (Exception e)
			{
				Logger.Log(eSeverity.Error, "Failed to Unsubscribe - {0}", e.Message);
				return 0;
			}
		}

		/// <summary>
		/// Publish the given topic.
		/// </summary>
		/// <param name="topic"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		public override ushort Publish(string topic, [NotNull] byte[] message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			if (!IsConnected)
			{
				Logger.Log(eSeverity.Error, "Unable to Publish - Client is not connected.");
				return 0;
			}

			string debug =
				new StringBuilder()
					.AppendFormat("Topic: {0}, ", topic)
					.AppendFormat("Message: {0}", Encoding.UTF8.GetString(message))
					.ToString();

			PrintTx("Publish", debug);

			try
			{
				return GetClient().Publish(topic, message);
			}
			catch (Exception e)
			{
				Logger.Log(eSeverity.Error, "Failed to Publish - {0}", e.Message);
				return 0;
			}
		}

		/// <summary>
		/// Publish the given topic.
		/// </summary>
		/// <param name="topic"></param>
		/// <param name="message"></param>
		/// <param name="qosLevel"></param>
		/// <param name="retain"></param>
		/// <returns></returns>
		public override ushort Publish(string topic, [NotNull] byte[] message, byte qosLevel, bool retain)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			if (!IsConnected)
			{
				Logger.Log(eSeverity.Error, "Unable to Publish - Client is not connected.");
				return 0;
			}

			string debug =
				new StringBuilder()
					.AppendFormat("Topic: {0}, ", topic)
					.AppendFormat("QOS Level: {0}, ", qosLevel)
					.AppendFormat("Retain: {0}, ", retain)
					.AppendFormat("Message: {0}", Encoding.UTF8.GetString(message))
					.ToString();

			PrintTx("Publish", debug);

			try
			{
				return GetClient().Publish(topic, message, qosLevel, retain);
			}
			catch (Exception e)
			{
				Logger.Log(eSeverity.Error, "Failed to Publish - {0}", e.Message);
				return 0;
			}
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

			MqttSslProtocols protocol =
				Secure
#if SIMPLSHARP
					? MqttSslProtocols.TLSv1_0
#else
					? MqttSslProtocols.TLSv1_2
#endif
					: MqttSslProtocols.None;

			X509Certificate caCert = null;

			string certPath = string.IsNullOrEmpty(CaCertPath) ? null : PathUtils.GetDefaultConfigPath("Certificates", CaCertPath);
			if (Secure && certPath != null)
			{
				if (IcdFile.Exists(certPath))
					caCert = new X509Certificate(certPath);
				else
					Logger.Log(eSeverity.Error, "No certificate found at path {0}", certPath);
			}

			// Null proxy hostname disables the proxy
			string proxyHostname = string.IsNullOrEmpty(ProxyHostname) ? null : ProxyHostname;

			return Client = new MqttClient(Hostname, Port, proxyHostname, ProxyPort, Secure, caCert, null, protocol, UserCertificateValidation, null);
		}

		private bool UserCertificateValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslpolicyerrors)
		{
			// TODO - Need to get cert validation working on crestron
			return true;
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