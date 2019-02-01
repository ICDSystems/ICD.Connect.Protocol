using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Protocol.Network.Broadcast;
using ICD.Connect.Protocol.Network.Ports.Tcp;
using ICD.Connect.Protocol.Network.Utils;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;

namespace ICD.Connect.Protocol.Network.Direct
{
	public delegate void ClientBufferCallback(IReply response);

	public sealed class DirectMessageManager : IDisposable
	{
		private readonly TcpClientPool m_ClientPool;

		private readonly AsyncTcpServer m_Server;
		private readonly TcpServerBufferManager m_ServerBuffer;

		private readonly Dictionary<Guid, ClientBufferCallback> m_MessageCallbacks;
		private readonly Dictionary<Type, IMessageHandler> m_MessageHandlers;
		private readonly SafeCriticalSection m_MessageHandlersSection;

		private readonly int m_SystemId;

		private BroadcastManager m_BroadcastManager;

		private BroadcastManager BroadcastManager
		{
			get { return m_BroadcastManager ?? (m_BroadcastManager = ServiceProvider.GetService<BroadcastManager>()); }
		}

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

			m_ServerBuffer = new TcpServerBufferManager(() => new DelimiterSerialBuffer(AbstractMessage.DELIMITER));
			m_ServerBuffer.SetServer(m_Server);
			Subscribe(m_ServerBuffer);

			m_ClientPool = new TcpClientPool();
		}

		#endregion

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			Unsubscribe(m_ServerBuffer);

			m_ServerBuffer.Dispose();

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
				throw new ArgumentNullException("type");

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
				throw new ArgumentNullException("type");

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

		public HostSessionInfo GetHostSessionInfo()
		{
			HostInfo host = GetHostInfo();
			Guid session = BroadcastManager.Session;

			return new HostSessionInfo(host, session);
		}

		#endregion

		#region Client Methods

		/// <summary>
		/// Sends the message to the address without receiving a response
		/// </summary>
		/// <param name="sendTo"></param>
		/// <param name="message"></param>
		public void Send(HostSessionInfo sendTo, IMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			Send(sendTo, message, null);
		}

		/// <summary>
		/// Sends the message to the address <code>sendTo</code>, using the callback to handle the response.
		/// </summary>
		/// <param name="sendTo"></param>
		/// <param name="message"></param>
		/// <param name="callback"></param>
		public void Send(HostSessionInfo sendTo, IMessage message, ClientBufferCallback callback)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			Guid messageId = Guid.NewGuid();

			message.MessageId = messageId;
			message.MessageFrom = GetHostSessionInfo();
			message.MessageTo = sendTo;

			string data = message.Serialize();

			AsyncTcpClient client = m_ClientPool.GetClient(sendTo.Host);
			if (!client.IsConnected)
				client.Connect();

			if (callback != null)
				m_MessageCallbacks.Add(messageId, callback);

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
			AbstractMessage message = null;

			try
			{
				message = AbstractMessage.Deserialize(data);
			}
			catch (Exception e)
			{
				ServiceProvider.TryGetService<ILoggerService>()
				               .AddEntry(eSeverity.Error, "{0} - Failed to deserialize message from {1} - {2}{3}{4}",
				                         GetType().Name, m_Server.GetClientInfo(clientId), e.Message, IcdEnvironment.NewLine, data);
			}

			if (message == null)
				return;

			// Handle reply callback
			IReply reply = message as IReply;
			ClientBufferCallback callback;
			if (reply != null && m_MessageCallbacks.TryGetValue(message.MessageId, out callback))
			{
				m_MessageCallbacks.Remove(reply.MessageId);
				callback(reply);
			}

			// Message handlers
			IMessageHandler handler;
			if (!m_MessageHandlers.TryGetValue(message.GetType(), out handler) || handler == null)
				return;

			IReply response = handler.HandleMessage(message);
			if (response == null)
				return;

			response.MessageId = message.MessageId;

			// Send the reply to the initial sender
			Send(message.MessageFrom, response);
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

			Send(reply.MessageTo, reply);
		}

		#endregion
	}
}
