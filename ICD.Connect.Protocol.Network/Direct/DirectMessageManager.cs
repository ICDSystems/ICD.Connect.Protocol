using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Network.Tcp;
using ICD.Connect.Protocol.Network.Utils;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;

namespace ICD.Connect.Protocol.Network.Direct
{
	public delegate void ClientBufferCallback(IReply response);

	public delegate void MessageResponseCallback<TResponse>(TResponse response) where TResponse : IReply;

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

			m_Server = new AsyncTcpServer(NetworkUtils.GetDirectMessagePortForSystem(m_SystemId), 64)
			{
				Name = GetType().Name
			};
			Subscribe(m_Server);

			m_ServerBuffer = new TcpServerBufferManager(() => new DelimiterSerialBuffer(AbstractMessage.DELIMITER));
			m_ServerBuffer.SetServer(m_Server);
			Subscribe(m_ServerBuffer);

			m_ClientPool = new TcpClientPool();
			m_ClientBuffers = new TcpClientPoolBufferManager(() => new DelimiterSerialBuffer(AbstractMessage.DELIMITER));
			m_ClientBuffers.SetPool(m_ClientPool);
			Subscribe(m_ClientBuffers);
		}

		#endregion

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			Unsubscribe(m_Server);
			Unsubscribe(m_ServerBuffer);
			Unsubscribe(m_ClientBuffers);

			m_ServerBuffer.Dispose();
			m_ClientBuffers.Dispose();

			m_MessageCallbacks.Clear();

			foreach (IMessageHandler handler in m_MessageHandlers.Values)
			{
				Unsubscribe(handler);
				handler.Dispose();
			}

			m_MessageHandlers.Clear();

			m_Server.Dispose();
			m_ClientPool.Dispose();
		}

		public void Start()
		{
			m_Server.Start();
		}

		public void Stop()
		{
			m_Server.Stop();
		}

		#region Server Methods

		/// <summary>
		/// Adds the given message handler to the manager.
		/// </summary>
		/// <param name="handler"></param>
		public void RegisterMessageHandler(IMessageHandler handler)
		{
			if (handler == null)
				throw new ArgumentNullException("handler");

			m_MessageHandlersSection.Enter();

			try
			{
				Type messageType = handler.MessageType;

				if (m_MessageHandlers.ContainsKey(messageType))
					throw new InvalidOperationException("Message handler already registered for type");

				m_MessageHandlers[messageType] = handler;
				Subscribe(handler);
			}
			finally
			{
				m_MessageHandlersSection.Leave();
			}
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
			if (type == null)
				throw new ArgumentNullException("handler");

			m_MessageHandlersSection.Enter();

			try
			{
				IMessageHandler handler;
				if (!m_MessageHandlers.TryGetValue(type, out handler))
					return;

				Unsubscribe(handler);

				m_MessageHandlers.Remove(type);
			}
			finally
			{
				m_MessageHandlersSection.Leave();
			}
		}

		public IMessageHandler GetMessageHandler<T>()
		{
			return GetMessageHandler(typeof(T));
		}

		private IMessageHandler GetMessageHandler(Type type)
		{
			if (type == null)
				throw new ArgumentNullException("handler");

			m_MessageHandlersSection.Enter();

			try
			{
				IMessageHandler handler;
				if (m_MessageHandlers.TryGetValue(type, out handler))
					return handler;

				throw new KeyNotFoundException();
			}
			finally
			{
				m_MessageHandlersSection.Leave();
			}
		}

		public HostInfo GetHostInfo()
		{
			return NetworkUtils.GetLocalHostInfo(m_SystemId);
		}

		#endregion

		#region Client Methods

		/// <summary>
		/// Sends the message to the address without receiving a response
		/// </summary>
		/// <param name="sendTo"></param>
		/// <param name="message"></param>
		public void Send(HostInfo sendTo, IMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

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
		public void Send<TResponse>(HostInfo sendTo, IMessage message, MessageResponseCallback<TResponse> callback)
			where TResponse : IReply
		{
			if (message == null)
				throw new ArgumentNullException("message");

			if (callback == null)
				throw new ArgumentNullException("callback");

			Guid messageId = Guid.NewGuid();

			message.MessageId = messageId;
			message.MessageFrom = GetHostInfo();
			string data = message.Serialize();

			AsyncTcpClient client = m_ClientPool.GetClient(sendTo);
			m_MessageCallbacks.Add(messageId, response => callback((TResponse)response));
			client.Send(data);
		}

		#endregion

		#region Server Callbacks

		/// <summary>
		/// Subscribe to the server events.
		/// </summary>
		/// <param name="server"></param>
		private void Subscribe(AsyncTcpServer server)
		{
			server.OnSocketStateChange += ServerOnOnSocketStateChange;
		}

		/// <summary>
		/// Unsubscribe from the server events.
		/// </summary>
		/// <param name="server"></param>
		private void Unsubscribe(AsyncTcpServer server)
		{
			server.OnSocketStateChange -= ServerOnOnSocketStateChange;
		}

		/// <summary>
		/// Called when a client connects/disconnects.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void ServerOnOnSocketStateChange(object sender, SocketStateEventArgs eventArgs)
		{
			if (m_Server.ClientConnected(eventArgs.ClientId))
				return;

			m_MessageHandlersSection.Enter();

			try
			{
				// Inform the handlers of a client disconnect.
				foreach (IMessageHandler handler in m_MessageHandlers.Values)
					handler.HandleClientDisconnect(eventArgs.ClientId);
			}
			finally
			{
				m_MessageHandlersSection.Leave();
			}
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

			IReply response = m_MessageHandlers[type].HandleMessage(msg);
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
			if (client == null)
				throw new ArgumentNullException("client");

			IReply message = AbstractMessage.Deserialize(data) as IReply;
			if (message == null)
				return;

			// Handle registered callbacks
			if (m_MessageCallbacks.ContainsKey(message.MessageId))
			{
				ClientBufferCallback callback = m_MessageCallbacks[message.MessageId];
				m_MessageCallbacks.Remove(message.MessageId);

				callback(message);
			}

			// Message handlers
			Type type = message.GetType();
			if (!m_MessageHandlers.ContainsKey(type))
				return;

			IReply response = m_MessageHandlers[type].HandleMessage(message);
			if (response == null)
				return;

			// Send the reply back to the server
			response.MessageId = message.MessageId;
			response.MessageFrom = GetHostInfo();
			client.Send(response.Serialize());
		}

		#endregion

		#region Handler Callbacks

		private void Subscribe(IMessageHandler handler)
		{
			handler.OnAsyncReply += HandlerOnAsyncReply;
		}

		private void Unsubscribe(IMessageHandler handler)
		{
			handler.OnAsyncReply -= HandlerOnAsyncReply;
		}

		private void HandlerOnAsyncReply(IMessageHandler sender, IReply reply)
		{
			if (reply == null)
				throw new ArgumentNullException("reply");

			if (reply.ClientId <= 0)
				throw new InvalidOperationException("Unable to send message to unknown client");

			if (!m_Server.ClientConnected(reply.ClientId))
				throw new InvalidOperationException("Unable to send message to disconnected client");

			reply.MessageFrom = GetHostInfo();
			m_Server.Send(reply.ClientId, reply.Serialize());
		}

		#endregion
	}
}
