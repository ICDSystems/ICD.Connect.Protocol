﻿using System;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Logging.LoggingContexts;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Network.Servers;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Utils;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace ICD.Connect.Protocol.NetworkPro.Ports.WebSockets
{
	public sealed class IcdWebSocketServer : AbstractNetworkServer
	{
		private readonly BiDictionary<uint, CallbackWebSocketBehavior> m_Sessions;
		private readonly SafeCriticalSection m_SessionsSection;
		
		[CanBeNull]
		private WebSocketServer m_Server;

		/// <summary>
		/// Constructor.
		/// </summary>
		public IcdWebSocketServer()
		{
			m_Sessions = new BiDictionary<uint, CallbackWebSocketBehavior>();
			m_SessionsSection = new SafeCriticalSection();
		}

		#region Methods

		/// <summary>
		/// Starts the Server
		/// </summary>
		public override void Start()
		{
			if (m_Server != null)
				throw new InvalidOperationException("Server must be stopped before it is started again.");

			Logger.Log(eSeverity.Notice, "Starting server");

			Enabled = true;

			m_Server = new WebSocketServer(Port);
			m_Server.AddWebSocketService<CallbackWebSocketBehavior>("/", AddSession);
			m_Server.Start();

			UpdateListeningState();

			Logger.Log(eSeverity.Notice, string.Format("Listening on port {0}", Port));
		}

		/// <summary>
		/// Stops the server.
		/// </summary>
		/// <param name="disable">When true disables the TCP server.</param>
		protected override void Stop(bool disable)
		{
			if (disable)
			{
				if (m_Server != null)
					Logger.Log(eSeverity.Notice, "Stopping server");
				Enabled = false;
			}
			else
			{
				if (m_Server != null)
					Logger.Log(eSeverity.Notice, "Temporarily stopping server");
			}

			if (m_Server != null)
			{
				try
				{
					m_Server.Stop();
				}
				finally
				{
					Logger.Log(eSeverity.Notice, "No longer listening on port {0}", m_Server.Port);

					m_Server = null;
					UpdateListeningState();
				}
			}

			foreach (uint client in GetClients())
				RemoveClient(client, SocketStateEventArgs.eSocketStatus.SocketStatusNoConnect);
		}

		/// <summary>
		/// Called in a worker thread to send the data to the specified client
		/// This should send the data synchronously to ensure in-order transmission
		/// If this blocks, it will stop all data from being sent
		/// </summary>
		/// <param name="clientId"></param>
		/// <param name="data"></param>
		protected override void SendWorkerAction(uint clientId, string data)
		{
			HostInfo hostInfo;
			if (!TryGetClientInfo(clientId, out hostInfo))
			{
				Logger.Log(eSeverity.Warning, "Unable to send data to unconnected client {0}", clientId);
				RemoveClient(clientId, SocketStateEventArgs.eSocketStatus.SocketStatusNoConnect);
				return;
			}

			byte[] byteData = StringUtils.ToBytes(data);

			PrintTx(hostInfo, data);

			m_SessionsSection.Execute(() => m_Sessions.GetValue(clientId)).Context.WebSocket.Send(byteData);
		}

		#endregion

		#region Private Methods

#if SIMPLSHARP
		/// <summary>
		/// Called when a client connects, starting a new session.
		/// </summary>
		/// <param name="behavior"></param>
		private void AddSession(CallbackWebSocketBehavior behavior)
		{
			m_SessionsSection.Enter();

			try
			{
				uint nextId = (uint)IdUtils.GetNewId(m_Sessions.Keys.Select(k => (int)k), 1);
				m_Sessions.Add(nextId, behavior);

				Subscribe(behavior);
			}
			finally
			{
				m_SessionsSection.Leave();
			}
		}
#else
		/// <summary>
		/// Called when a client connects, starting a new session.
		/// </summary>
		private CallbackWebSocketBehavior AddSession()
		{
			CallbackWebSocketBehavior behavior = new CallbackWebSocketBehavior();

			m_SessionsSection.Enter();

			try
			{
				uint nextId = (uint)IdUtils.GetNewId(m_Sessions.Keys.Select(k => (int)k), 1);
				m_Sessions.Add(nextId, behavior);

				Subscribe(behavior);
			}
			finally
			{
				m_SessionsSection.Leave();
			}

			return behavior;
		}
#endif

		/// <summary>
		/// Updates the listening property to match the server.
		/// </summary>
		private void UpdateListeningState()
		{
			Listening = m_Server != null && m_Server.IsListening;
		}

		#endregion

		#region Behavior Callbacks

		/// <summary>
		/// Subscribe to the callback behavior events.
		/// </summary>
		/// <param name="callbackBehavior"></param>
		private void Subscribe(CallbackWebSocketBehavior callbackBehavior)
		{
			if (callbackBehavior == null)
				return;

			callbackBehavior.OnOpened += CallbackBehaviorOnOpened;
			callbackBehavior.OnClosed += CallbackBehaviorOnClosed;
			callbackBehavior.OnErrored += CallbackBehaviorOnErrored;
			callbackBehavior.OnMessageReceived += CallbackBehaviorOnMessageReceived;
		}

		/// <summary>
		/// Unsubscribe from the callback behavior events.
		/// </summary>
		/// <param name="callbackBehavior"></param>
		private void Unsubscribe(CallbackWebSocketBehavior callbackBehavior)
		{
			if (callbackBehavior == null)
				return;

			callbackBehavior.OnOpened += CallbackBehaviorOnOpened;
			callbackBehavior.OnClosed += CallbackBehaviorOnClosed;
			callbackBehavior.OnErrored += CallbackBehaviorOnErrored;
			callbackBehavior.OnMessageReceived += CallbackBehaviorOnMessageReceived;
		}

		/// <summary>
		/// Called when a session opens.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void CallbackBehaviorOnOpened(object sender, EventArgs eventArgs)
		{
			CallbackWebSocketBehavior session = (CallbackWebSocketBehavior)sender;

			string host = session.Context.UserEndPoint.Address.ToString();
			ushort port = (ushort)session.Context.UserEndPoint.Port;
			HostInfo hostInfo = new HostInfo(host, port);

			uint id = m_SessionsSection.Execute(() => m_Sessions.GetKey(session));

			AddClient(id, hostInfo, SocketStateEventArgs.eSocketStatus.SocketStatusConnected);
		}

		/// <summary>
		/// Called when a session closes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="closeEventArgs"></param>
		private void CallbackBehaviorOnClosed(object sender, CloseEventArgs closeEventArgs)
		{
			CallbackWebSocketBehavior session = (CallbackWebSocketBehavior)sender;
			Unsubscribe(session);

			uint id;

			m_SessionsSection.Enter();

			try
			{
				if (!m_Sessions.TryGetKey(session, out id))
					return;

				m_Sessions.RemoveKey(id);
			}
			finally
			{
				m_SessionsSection.Leave();
			}
			
			RemoveClient(id, SocketStateEventArgs.eSocketStatus.SocketStatusBrokenRemotely);
		}

		/// <summary>
		/// Called when a session throws an internal exception.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="errorEventArgs"></param>
		private void CallbackBehaviorOnErrored(object sender, ErrorEventArgs errorEventArgs)
		{
			Logger.Log(eSeverity.Error, errorEventArgs.Exception,
			           "WebSocketBehavior threw exception - {0}", errorEventArgs.Message);

			UpdateListeningState();
		}

		/// <summary>
		/// Called when a session receives a message from the client.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="messageEventArgs"></param>
		private void CallbackBehaviorOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
		{
			CallbackWebSocketBehavior session = (CallbackWebSocketBehavior)sender;
			uint clientId = m_SessionsSection.Execute(() => m_Sessions.GetKey(session));

			HostInfo hostInfo;
			if (!TryGetClientInfo(clientId, out hostInfo))
				return;

			byte[] message = messageEventArgs.RawData;
			DataReceiveEventArgs eventArgs = new DataReceiveEventArgs(clientId, message, message.Length);

			PrintRx(hostInfo, eventArgs.Data);
			RaiseOnDataReceived(eventArgs);
		}

		#endregion
	}
}
