using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Connect.Protocol.Network.Tcp;
using ICD.Connect.Protocol.Network.Utils;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;
using Newtonsoft.Json;

namespace ICD.Connect.Protocol.Network.Direct
{
	public delegate void ClientBufferCallback(AbstractMessage response);

	public delegate void MessageResponseCallback<TResponse>(TResponse response) where TResponse : AbstractMessage;

	public sealed class DirectMessageManager : IDisposable
	{
		// stores clients and buffers so they do not get garbage collected
		private readonly TcpClientPool m_ClientPool;
		private readonly TcpClientPoolBufferManager m_ClientBuffers;

		private readonly Dictionary<Guid, ClientBufferCallback> m_MessageCallbacks;
		private readonly Dictionary<Type, IMessageHandler> m_MessageHandlers;
		private readonly SafeCriticalSection m_MessageHandlersSection;
		private readonly AsyncTcpServer m_Server;
		private readonly TcpServerBufferManager m_ServerBuffer;
		private readonly int m_SystemId;

		/// <summary>
		/// Creates a DirectMessageManager with default systemId of 0
		/// </summary>
		public DirectMessageManager() : this(0)
		{
		}

		/// <summary>
		/// Creates a DirectMessageManager with the given system ID. 
		/// DirectMessageManagers on different system IDs cannot communicate with each other.
		/// </summary>
		/// <param name="systemId"></param>
		public DirectMessageManager(int systemId)
		{
			m_SystemId = systemId;
			m_Server = new AsyncTcpServer(NetworkUtils.GetDirectMessagePortForSystem(systemId), 64);

			m_ServerBuffer = new TcpServerBufferManager(() => new DelimiterSerialBuffer(AbstractMessage.DELIMITER));
			m_ServerBuffer.SetServer(m_Server);
			m_ServerBuffer.OnClientCompletedSerial += ServerBufferOnClientCompletedSerial;

			m_MessageHandlers = new Dictionary<Type, IMessageHandler>();
			m_MessageHandlersSection = new SafeCriticalSection();

			m_ClientPool = new TcpClientPool();
			m_ClientBuffers = new TcpClientPoolBufferManager(() => new DelimiterSerialBuffer(AbstractMessage.DELIMITER));
			m_ClientBuffers.SetPool(m_ClientPool);
			m_ClientBuffers.OnClientCompletedSerial += ClientPoolBufferOnClientCompletedSerial;

			m_MessageCallbacks = new Dictionary<Guid, ClientBufferCallback>();

			m_Server.Start();
		}

		private void ServerBufferOnClientCompletedSerial(TcpServerBufferManager sender, uint clientId, string data)
		{
			AbstractMessage msg = AbstractMessage.Deserialize(data);
			if (msg == null)
				return;
			msg.ClientId = clientId;
			Type type = msg.GetType();
			if (m_MessageHandlers.ContainsKey(type))
			{
				AbstractMessage response = m_MessageHandlers[type].HandleMessage(msg);
				if (response != null && m_Server.ClientConnected(clientId))
				{
					response.MessageId = msg.MessageId;
					response.MessageFrom = GetHostInfo();
					m_Server.Send(clientId, response.Serialize());
				}
			}
		}

		#region Server Methods

		public void RegisterMessageHandler<TMessage>(AbstractMessageHandler<TMessage> handler)
			where TMessage : AbstractMessage, new()
		{
			Type t = typeof(TMessage);
			m_MessageHandlersSection.Enter();
			if (!m_MessageHandlers.ContainsKey(t))
				m_MessageHandlers.Add(t, handler);
			else
				m_MessageHandlers[t] = handler;

			// cache serialization info
			string serialized = JsonConvert.SerializeObject(ReflectionUtils.CreateInstance<TMessage>());
			JsonConvert.DeserializeObject<TMessage>(serialized);

			m_MessageHandlersSection.Leave();
		}

		public void UnregisterMessageHandler<TMessage>()
		{
			UnregisterMessageHandler(typeof(TMessage));
		}

		public void UnregisterMessageHandler(Type t)
		{
			m_MessageHandlersSection.Execute(() => m_MessageHandlers.Remove(t));
		}

		public HostInfo GetHostInfo()
		{
			string address = IcdEnvironment.NetworkAddresses.FirstOrDefault();
			ushort port = NetworkUtils.GetDirectMessagePortForSystem(m_SystemId);

			return new HostInfo(address, port);
		}

		public void Respond(uint clientId, Guid originalMessageId, AbstractMessage response)
		{
			if (clientId > 0 && response != null && m_Server.ClientConnected(clientId))
			{
				response.MessageId = originalMessageId;
				response.MessageFrom = GetHostInfo();
				m_Server.Send(clientId, response.Serialize());
			}
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

		public void Dispose()
		{
			m_MessageCallbacks.Clear();

			foreach (IDisposable handler in m_MessageHandlers.Values.OfType<IDisposable>())
				handler.Dispose();

			m_MessageHandlers.Clear();
			m_Server.Dispose();
			m_ClientPool.Dispose();
		}
	}
}
