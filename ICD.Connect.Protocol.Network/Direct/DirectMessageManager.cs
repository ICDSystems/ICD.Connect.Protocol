using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Network.Broadcast;
using ICD.Connect.Protocol.Network.Ports.Tcp;
using ICD.Connect.Protocol.Network.Utils;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;

namespace ICD.Connect.Protocol.Network.Direct
{
	public sealed class DirectMessageManager : IDisposable, IConsoleNode
	{
		private readonly TcpClientPool m_ClientPool;

		private readonly AsyncTcpServer m_Server;
		private readonly TcpServerBufferManager m_ServerBuffer;

		private readonly Dictionary<Guid, ClientBufferCallbackInfo> m_MessageCallbacks;
		private readonly Dictionary<Type, IMessageHandler> m_MessageHandlers;
		private readonly SafeCriticalSection m_MessageHandlersSection;

		private readonly int m_SystemId;

		private BroadcastManager m_BroadcastManager;
		private ILoggerService m_CachedLogger;

		#region Properties

		private BroadcastManager BroadcastManager
		{
			get { return m_BroadcastManager ?? (m_BroadcastManager = ServiceProvider.GetService<BroadcastManager>()); }
		}

		private ILoggerService Logger
		{
			get { return m_CachedLogger = m_CachedLogger ?? ServiceProvider.TryGetService<ILoggerService>(); }
		}

		#endregion

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
			m_MessageCallbacks = new Dictionary<Guid, ClientBufferCallbackInfo>();
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

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			Unsubscribe(m_ServerBuffer);

			m_ServerBuffer.Dispose();

			m_MessageHandlersSection.Enter();

			try
			{
				foreach (KeyValuePair<Guid, ClientBufferCallbackInfo> kvp in m_MessageCallbacks)
					kvp.Value.Dispose();
				m_MessageCallbacks.Clear();

				foreach (IMessageHandler handler in m_MessageHandlers.Values)
				{
					Unsubscribe(handler);
					handler.Dispose();
				}

				m_MessageHandlers.Clear();
			}
			finally
			{
				m_MessageHandlersSection.Leave();
			}

			m_Server.Dispose();
			m_ClientPool.Dispose();
		}

		#endregion

		#region Methods

		public void Start()
		{
			m_Server.Start();
		}

		public void Stop()
		{
			m_Server.Stop();
		}

		#endregion

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

				m_MessageHandlers.Add(messageType, handler);
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

				throw new KeyNotFoundException(string.Format("No message handler registered for messages of type {0}", type.Name));
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

			Send<IMessage, IReply>(sendTo, message, null, null, 0);
		}

		/// <summary>
		/// Sends the message to the address <code>sendTo</code>, using the callback to handle the response.
		/// </summary>
		/// <param name="sendTo"></param>
		/// <param name="message"></param>
		/// <param name="replyCallback"></param>
		/// <param name="timeoutCallback"></param>
		/// <param name="timeout"></param>
		public void Send<TMessage, TReply>(HostSessionInfo sendTo, TMessage message, Action<TReply> replyCallback, Action<TMessage> timeoutCallback, long timeout)
			where TMessage : IMessage
			where TReply : IReply
		{
			if (message == null)
				throw new ArgumentNullException("message");

			Guid messageId = Guid.NewGuid();

			message.MessageId = messageId;
			message.MessageFrom = GetHostSessionInfo();
			message.MessageTo = sendTo;

			// Called AFTER assigning a message id
			if (replyCallback != null)
				ExpectReply(message, replyCallback, timeoutCallback, timeout);

			string data = message.Serialize();

			AsyncTcpClient client = m_ClientPool.GetClient(sendTo.Host);
			if (!client.IsConnected)
				client.Connect();

			client.Send(data);
		}

		/// <summary>
		/// Registers a reply callback with the given timeout duration.
		/// </summary>
		/// <typeparam name="TMessage"></typeparam>
		/// <typeparam name="TReply"></typeparam>
		/// <param name="message"></param>
		/// <param name="replyCallback"></param>
		/// <param name="timeoutCallback"></param>
		/// <param name="timeout"></param>
		private void ExpectReply<TMessage, TReply>(TMessage message, Action<TReply> replyCallback, Action<TMessage> timeoutCallback, long timeout)
			where TMessage : IMessage
			where TReply : IReply
		{
			if (message == null)
				throw new ArgumentNullException("message");

			if (replyCallback == null)
				throw new ArgumentNullException("replyCallback");

			m_MessageHandlersSection.Enter();

			try
			{
				ClientBufferCallbackInfo callbackInfo =
					new ClientBufferCallbackInfo(message,
					                             r => replyCallback((TReply)r),
					                             m => HandleReplyTimeout((TMessage)m, timeoutCallback));

				m_MessageCallbacks.Add(message.MessageId, callbackInfo);

				if (timeout > 0)
					callbackInfo.ResetTimer(timeout);
			}
			finally
			{
				m_MessageHandlersSection.Leave();
			}
		}

		/// <summary>
		/// Called when a message times out with no reply.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="timeoutCallback"></param>
		private void HandleReplyTimeout<TMessage>(TMessage message, Action<TMessage> timeoutCallback)
			where TMessage : IMessage
		{
			ClientBufferCallbackInfo callbackInfo;

			m_MessageHandlersSection.Enter();

			try
			{
				if (!m_MessageCallbacks.TryGetValue(message.MessageId, out callbackInfo))
					return;

				m_MessageCallbacks.Remove(message.MessageId);
			}
			finally
			{
				m_MessageHandlersSection.Leave();
			}

			callbackInfo.Dispose();

			Logger.AddEntry(eSeverity.Error, "{0} - Message timed out - {1}", GetType().Name, message);
			if (timeoutCallback != null)
				timeoutCallback(message);
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
			IMessage message = null;

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

			if (message != null)
				HandleMessage(message);
		}

		/// <summary>
		/// Handles the deserialized message from a remote endpoint.
		/// </summary>
		/// <param name="message"></param>
		private void HandleMessage(IMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			IReply reply = message as IReply;
			IMessageHandler handler;
			ClientBufferCallbackInfo callbackInfo = null;

			m_MessageHandlersSection.Enter();

			try
			{
				if (reply != null && m_MessageCallbacks.TryGetValue(message.MessageId, out callbackInfo))
					m_MessageCallbacks.Remove(reply.MessageId);

				handler = m_MessageHandlers.GetDefault(message.GetType());
			}
			finally
			{
				m_MessageHandlersSection.Leave();
			}

			// Handle expected reply
			if (callbackInfo != null)
			{
				callbackInfo.HandleReply(reply);
				callbackInfo.Dispose();
			}

			// Handle message
			IReply response = handler == null ? null : handler.HandleMessage(message);
			if (response == null)
				return;

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

		#region Console

		public string ConsoleName { get { return GetType().Name; } }

		public string ConsoleHelp
		{
			get { return "Handles direct communication between Cores."; }
		}

		public IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield return m_ClientPool;
			yield return m_Server;
		}

		public void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("System ID", m_SystemId);
			addRow("Host Session Info", GetHostSessionInfo());
			addRow("Active", m_Server.Active);
		}

		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new ConsoleCommand("Start", "Resumes receiving messages", () => Start());
			yield return new ConsoleCommand("Stop", "Stops receiving messages", () => Stop());
		}

		#endregion
	}
}
