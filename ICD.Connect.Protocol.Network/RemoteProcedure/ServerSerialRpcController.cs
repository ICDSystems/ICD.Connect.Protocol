﻿#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using System;
using ICD.Common.Properties;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Protocol.Network.Servers;
using ICD.Connect.Protocol.SerialBuffers;

namespace ICD.Connect.Protocol.Network.RemoteProcedure
{
	/// <summary>
	/// The ServerSerialRpcController simplifies using RPCs over a serial connection.
	/// </summary>
	[PublicAPI]
	public sealed class ServerSerialRpcController : IDisposable
	{
		public const char DELIMITER = '\xFF';

		private readonly NetworkServerBufferManager m_BufferManager;
		private readonly object m_Parent;

		private INetworkServer m_Server;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		public ServerSerialRpcController(object parent)
		{
			m_BufferManager = new NetworkServerBufferManager(() => new DelimiterSerialBuffer(DELIMITER));
			m_Parent = parent;

			Subscribe(m_BufferManager);
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			SetServer(null);
			Unsubscribe(m_BufferManager);
		}

		/// <summary>
		/// Calls the method on all clients.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="parameters"></param>
		[PublicAPI]
		public void CallMethod(string key, params object[] parameters)
		{
			string data = Rpc.CallMethodRpc(key, parameters).Serialize();
			SendData(null, data);
		}

		/// <summary>
		/// Calls the method on the client.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="key"></param>
		/// <param name="parameters"></param>
		[PublicAPI]
		public void CallMethod(uint client, string key, params object[] parameters)
		{
			string data = Rpc.CallMethodRpc(key, parameters).Serialize();
			SendData(client, data);
		}

		/// <summary>
		/// Sets the property on all clients.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		[PublicAPI]
		public void SetProperty(string key, object value)
		{
			string data = Rpc.SetPropertyRpc(key, value).Serialize();
			SendData(null, data);
		}

		/// <summary>
		/// Sets the property on the client.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="key"></param>
		/// <param name="value"></param>
		[PublicAPI]
		public void SetProperty(uint client, string key, object value)
		{
			string data = Rpc.SetPropertyRpc(key, value).Serialize();
			SendData(client, data);
		}

		/// <summary>
		/// Sends the serial data to the client.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="data"></param>
		private void SendData(uint? client, string data)
		{
			if (m_Server == null)
			{
				ServiceProvider.TryGetService<ILoggerService>()
				               .AddEntry(eSeverity.Error, "{0} unable to send data, server is null", GetType().Name);
				return;
			}

			if (client.HasValue && !m_Server.ClientConnected(client.Value))
			{
				ServiceProvider.TryGetService<ILoggerService>()
				               .AddEntry(eSeverity.Warning, "{0} unable to send data, no client {1}", GetType().Name, client);
				return;
			}

			if (client.HasValue)
				m_Server.Send(client.Value, data + DELIMITER);
			else
				m_Server.Send(data + DELIMITER);
		}

		/// <summary>
		/// Sets the server for communication with the client RPC controllers.
		/// </summary>
		/// <param name="server"></param>
		[PublicAPI]
		public void SetServer(INetworkServer server)
		{
			if (server == m_Server)
				return;

			m_Server = server;

			if (m_Server != null)
				m_Server.Name = m_Server.Name ?? GetType().Name;

			m_BufferManager.SetServer(m_Server);
		}

		#endregion

		#region Buffer Callbacks

		/// <summary>
		/// Subscribe to the buffer manager events.
		/// </summary>
		/// <param name="bufferManager"></param>
		private void Subscribe(NetworkServerBufferManager bufferManager)
		{
			bufferManager.OnClientCompletedSerial += BufferManagerOnClientCompletedSerial;
		}

		/// <summary>
		/// Unsubscribe from the buffer manager events.
		/// </summary>
		/// <param name="bufferManager"></param>
		private void Unsubscribe(NetworkServerBufferManager bufferManager)
		{
			bufferManager.OnClientCompletedSerial -= BufferManagerOnClientCompletedSerial;
		}

		/// <summary>
		/// Called when we get a complete JSON string from a client.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="clientId"></param>
		/// <param name="data"></param>
		private void BufferManagerOnClientCompletedSerial(NetworkServerBufferManager sender, uint clientId, string data)
		{
			Rpc rpc = JsonConvert.DeserializeObject<Rpc>(data);

			// We add the clientId to the start of the list of RPC parameters.
			if (rpc.ProcedureType == Rpc.eProcedureType.Method)
				rpc.PrependClientId(clientId);

			rpc.Execute(m_Parent);
		}

		#endregion
	}
}
