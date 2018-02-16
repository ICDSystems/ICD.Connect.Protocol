using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Json;
using ICD.Connect.Protocol.Network.Tcp;
using ICD.Connect.Protocol.Network.Utils;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;

namespace ICD.Connect.Protocol.Network.Direct
{
	public delegate void ClientBufferCallback(AbstractMessage response);

	public delegate void MessageResponseCallback<TResponse>(TResponse response) where TResponse : AbstractMessage;

	public sealed class DirectMessageManager : IDisposable
	{
		private readonly TcpClientPool m_ClientPool;
		private readonly TcpClientPoolBufferManager m_ClientBuffers;

		private readonly AsyncTcpServer m_Server;
		private readonly TcpServerBufferManager m_ServerBuffer;

		private readonly Dictionary<Guid, ClientBufferCallback> m_MessageCallbacks;
		private readonly Dictionary<Type, IMessageHandler> m_MessageHandlers;
		private readonly SafeCriticalSection m_MessageHandlersSection;

		private readonly int m_SystemId;

		#region Constructors

		/// <summary>
		/// Creates a DirectMessageManager with default systemId of 0
		/// </summary>
		[PublicAPI]
		public DirectMessageManager()
			: this(0)
		{
		}

		/// <summary>
		/// Creates a DirectMessageManager with the given system ID. 
		/// DirectMessageManagers on different system IDs cannot communicate with each other.
		/// </summary>
		/// <param name="systemId"></param>
		[PublicAPI]
		public DirectMessageManager(int systemId)
		{
			m_MessageHandlers = new Dictionary<Type, IMessageHandler>();
			m_MessageCallbacks = new Dictionary<Guid, ClientBufferCallback>();
			m_MessageHandlersSection = new SafeCriticalSection();

			m_SystemId = systemId;

			m_Server = new AsyncTcpServer(NetworkUtils.GetDirectMessagePortForSystem(systemId), 64)
			{
				Name = GetType().Name
			};
			m_ServerBuffer = new TcpServerBufferManager(() => new DelimiterSerialBuffer(AbstractMessage.DELIMITER));
			m_ServerBuffer.SetServer(m_Server);
			Subscribe(m_ServerBuffer);

			m_ClientPool = new TcpClientPool();
			m_ClientBuffers = new TcpClientPoolBufferManager(() => new DelimiterSerialBuffer(AbstractMessage.DELIMITER));
			m_ClientBuffers.SetPool(m_ClientPool);
			Subscribe(m_ClientBuffers);

			m_Server.Start();
		}

		#endregion

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			Unsubscribe(m_ServerBuffer);
			m_ServerBuffer.Dispose();

			Unsubscribe(m_ClientBuffers);
			m_ClientBuffers.Dispose();

			m_MessageCallbacks.Clear();

			foreach (IDisposable handler in m_MessageHandlers.Values.OfType<IDisposable>())
				handler.Dispose();
			m_MessageHandlers.Clear();

			m_Server.Dispose();
			m_ClientPool.Dispose();
		}

		#region Server Methods

		/// <summary>
		/// Adds the given message handler to the manager.
		/// </summary>
		/// <typeparam name="TMessage"></typeparam>
		/// <param name="handler"></param>
		public void RegisterMessageHandler<TMessage>(AbstractMessageHandler<TMessage> handler)
			where TMessage : AbstractMessage
		{
			m_MessageHandlersSection.Execute(() => m_MessageHandlers[typeof(TMessage)] = handler);

			if (ReflectionUtils.HasPublicParameterlessConstructor(typeof(TMessage)))
				JsonUtils.CacheType(typeof(TMessage));
		}

		/// <summary>
		/// Removes the message handler of the given type from the manager.
		/// </summary>
		/// <typeparam name="TMessage"></typeparam>
		public void UnregisterMessageHandler<TMessage>()
			where TMessage : AbstractMessage, new()
		{
			UnregisterMessageHandler(typeof(TMessage));
		}

		/// <summary>
		/// Removes the message handler of the given type from the manager.
		/// </summary>
		/// <param name="type"></param>
		public void UnregisterMessageHandler(Type type)
		{
			m_MessageHandlersSection.Execute(() => m_MessageHandlers.Remove(type));
		}

		public HostInfo GetHostInfo()
		{
			return NetworkUtils.GetLocalHostInfo(m_SystemId);
		}

		public void Respond(uint clientId, Guid originalMessageId, AbstractMessage response)
		{
			if (clientId <= 0 || response == null || !m_Server.ClientConnected(clientId))
				return;

			response.MessageId = originalMessageId;
			response.MessageFrom = GetHostInfo();
			m_Server.Send(clientId, response.Serialize());
		}

		#endregion

		#region Client Methods

		/// <summary>
		/// Sends the message to the address without receiving a response
		/// </summary>
		/// <param name="sendTo"></param>
		/// <param name="message"></param>
		public void Send(HostInfo sendTo, AbstractMessage message)
		{
			message.MessageId = Guid.NewGuid();
			message.MessageFrom = GetHostInfo();
			string data = message.Serialize();

			AsyncTcpClient client = m_ClientPool.GetClient(sendTo);
			client.Send(data);
		}

		/// <summary>
		/// Sends the message to the address <code>sendTo</code>, expecting a response of type TResponse
		/// </summary>
		/// <typeparam name="TResponse"></typeparam>
		/// <param name="sendTo"></param>
		/// <param name="message"></param>
		/// <param name="callback"></param>
		public void Send<TResponse>(HostInfo sendTo, AbstractMessage message, MessageResponseCallback<TResponse> callback)
			where TResponse : AbstractMessage
		{
			Guid messageId = Guid.NewGuid();

			message.MessageId = messageId;
			message.MessageFrom = GetHostInfo();
			string data = message.Serialize();

			AsyncTcpClient client = m_ClientPool.GetClient(sendTo);
			m_MessageCallbacks.Add(messageId, response => callback(response as TResponse));
			client.Send(data);
		}

		#endregion

		#region Server Buffer Callbacks

		/// <summary>
		/// Subscribe to the server buffer manager events.
		/// </summary>
		/// <param name="manager"></param>
		private void Subscribe(TcpServerBufferManager manager)
		{
			manager.OnClientCompletedSerial += ServerBufferOnClientCompletedSerial;
		}

		/// <summary>
		/// Unsubscribe from the server buffer manager events.
		/// </summary>
		/// <param name="manager"></param>
		private void Unsubscribe(TcpServerBufferManager manager)
		{
			manager.OnClientCompletedSerial -= ServerBufferOnClientCompletedSerial;
		}

		/// <summary>
		/// Called when a buffer completes a serial response.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="clientId"></param>
		/// <param name="data"></param>
		private void ServerBufferOnClientCompletedSerial(TcpServerBufferManager sender, uint clientId, string data)
		{
			AbstractMessage msg = AbstractMessage.Deserialize(data);
			if (msg == null)
				return;

			msg.ClientId = clientId;

			Type type = msg.GetType();
			if (!m_MessageHandlers.ContainsKey(type))
				return;

			AbstractMessage response = m_MessageHandlers[type].HandleMessage(msg);
			if (response == null || !m_Server.ClientConnected(clientId))
				return;

			response.MessageId = msg.MessageId;
			response.MessageFrom = GetHostInfo();
			m_Server.Send(clientId, response.Serialize());
		}

		#endregion

		#region Client Buffer Callbacks

		/// <summary>
		/// Subscribe to the client buffer manager events.
		/// </summary>
		/// <param name="manager"></param>
		private void Subscribe(TcpClientPoolBufferManager manager)
		{
			manager.OnClientCompletedSerial += ClientPoolBufferOnClientCompletedSerial;
		}

		/// <summary>
		/// Unsubscribe from the client buffer manager events.
		/// </summary>
		/// <param name="manager"></param>
		private void Unsubscribe(TcpClientPoolBufferManager manager)
		{
			manager.OnClientCompletedSerial -= ClientPoolBufferOnClientCompletedSerial;
		}

		/// <summary>
		/// Called when a buffer completes a serial response.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="client"></param>
		/// <param name="data"></param>
		private void ClientPoolBufferOnClientCompletedSerial(TcpClientPoolBufferManager sender, AsyncTcpClient client,
		                                                     string data)
		{
			AbstractMessage message = AbstractMessage.Deserialize(data);
			ClientBufferCallback callback = null;

			if (m_MessageCallbacks.ContainsKey(message.MessageId))
			{
				callback = m_MessageCallbacks[message.MessageId];
				m_MessageCallbacks.Remove(message.MessageId);
			}

			if (callback != null)
				callback(message);
		}

		#endregion
	}
}
