using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Timers;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.Network.Tcp
{
	/// <summary>
	/// The TcpClientPool provides a way to share existing TCP clients.
	/// </summary>
	public sealed class TcpClientPool : IDisposable
	{
		public delegate void ClientCallback(TcpClientPool sender, AsyncTcpClient client);

		public delegate void ClientConnectionStateCallback(TcpClientPool sender, AsyncTcpClient client, bool connected);

		public delegate void ClientSerialDataCallback(TcpClientPool sender, AsyncTcpClient client, string data);

		/// <summary>
		/// Raised when a client is added to the pool.
		/// </summary>
		public event ClientCallback OnClientAdded;

		/// <summary>
		/// Raised when a client is removed from the pool.
		/// </summary>
		public event ClientCallback OnClientRemoved;

		/// <summary>
		/// Raised when a client connects or disconnects.
		/// </summary>
		public event ClientConnectionStateCallback OnClientConnectionStateChanged;

		/// <summary>
		/// Raised when a client receives data.
		/// </summary>
		public event ClientSerialDataCallback OnClientSerialDataReceived;

		private readonly Dictionary<string, AsyncTcpClient> m_Clients;
		private readonly Dictionary<AsyncTcpClient, SafeTimer> m_ClientDisposalTimers;
		private readonly SafeCriticalSection m_ClientsSection;

		/// <summary>
		/// Constructor.
		/// </summary>
		public TcpClientPool()
		{
			m_Clients = new Dictionary<string, AsyncTcpClient>();
			m_ClientDisposalTimers = new Dictionary<AsyncTcpClient, SafeTimer>();
			m_ClientsSection = new SafeCriticalSection();
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			Clear();
		}

		/// <summary>
		/// Removes and disposes all of the stored TCP clients.
		/// </summary>
		[PublicAPI]
		public void Clear()
		{
			string[] keys = m_ClientsSection.Execute(() => m_Clients.Keys.ToArray());
			foreach (string key in keys)
				RemoveClient(key);
		}

		/// <summary>
		/// Retrieves the existing client with the given address:port.
		/// If no client exists, creates a new one.
		/// </summary>
		/// <param name="address"></param>
		/// <param name="port"></param>
		/// <returns></returns>
		[PublicAPI]
		public AsyncTcpClient GetClient(string address, ushort port)
		{
			string key = GetKey(address, port);

			m_ClientsSection.Enter();

			try
			{
				// Create a new client
				if (!m_Clients.ContainsKey(key))
					return InstantiateClient(address, port);

				// Return the cached client
				AsyncTcpClient client = m_Clients[key];
				StopDisposeTimer(client);
				return client;
			}
			finally
			{
				m_ClientsSection.Leave();
			}
		}

		/// <summary>
		/// Retrieves the existing client with the given address:port.
		/// If no client exists, creates a new one.
		/// </summary>
		/// <param name="host"></param>
		/// <returns></returns>
		[PublicAPI]
		public AsyncTcpClient GetClient(HostInfo host)
		{
			return GetClient(host.AddressOrLocalhost, host.Port);
		}

		/// <summary>
		/// Removes the TCP client with the given address and port.
		/// </summary>
		/// <param name="address"></param>
		/// <param name="port"></param>
		/// <returns>True if the client exists and was removed.</returns>
		[PublicAPI]
		public bool RemoveClient(string address, ushort port)
		{
			string key = GetKey(address, port);
			return RemoveClient(key);
		}

		/// <summary>
		/// Removes the TCP client from the pool.
		/// </summary>
		/// <param name="client"></param>
		/// <returns>True if the client exists and was removed.</returns>
		[PublicAPI]
		public bool RemoveClient(AsyncTcpClient client)
		{
			Unsubscribe(client);

			m_ClientsSection.Enter();

			try
			{
				if (!m_Clients.RemoveValue(client))
					return false;
			}
			finally
			{
				m_ClientsSection.Leave();
			}

			ClientCallback handler = OnClientRemoved;
			if (handler != null)
				handler(this, client);

			return true;
		}

		/// <summary>
		/// Removes and disposes the TCP client from the pool.
		/// </summary>
		/// <param name="client">True if the client exists and was removed and disposed.</param>
		[PublicAPI]
		public bool DisposeClient(AsyncTcpClient client)
		{
			bool output = RemoveClient(client);
			if (client != null)
				client.Dispose();

			return output;
		}

		/// <summary>
		/// Keeps the client in the pool for the given period of time (milliseconds).
		/// Once the time elapses the client is removed and disposed from the pool.
		/// 
		/// Any GetClient() call that returns the client will cancel the timer.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="keepAlive"></param>
		[PublicAPI]
		public void DisposeClient(AsyncTcpClient client, long keepAlive)
		{
			RestartDisposeTimer(client, keepAlive);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Instantiates/restarts the dispose timer for the given client.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="keepAlive"></param>
		private void RestartDisposeTimer(AsyncTcpClient client, long keepAlive)
		{
			m_ClientsSection.Enter();

			try
			{
				if (!m_ClientDisposalTimers.ContainsKey(client))
					m_ClientDisposalTimers[client] = SafeTimer.Stopped(() => DisposeClient(client));
				m_ClientDisposalTimers[client].Reset(keepAlive);
			}
			finally
			{
				m_ClientsSection.Leave();
			}
		}

		/// <summary>
		/// If the client is currently scheduled for disposal we end the timer.
		/// </summary>
		/// <param name="client"></param>
		private void StopDisposeTimer(AsyncTcpClient client)
		{
			m_ClientsSection.Enter();

			try
			{
				if (!m_ClientDisposalTimers.ContainsKey(client))
					return;

				m_ClientDisposalTimers[client].Dispose();
				m_ClientDisposalTimers.Remove(client);
			}
			finally
			{
				m_ClientsSection.Leave();
			}
		}

		/// <summary>
		/// Instantiates a new client for this given address:port.
		/// </summary>
		/// <param name="address"></param>
		/// <param name="port"></param>
		/// <returns></returns>
		private AsyncTcpClient InstantiateClient(string address, ushort port)
		{
			string key = GetKey(address, port);
			AsyncTcpClient output;

			m_ClientsSection.Enter();

			try
			{
				// Instantiate the new client
				output = new AsyncTcpClient
				{
					Address = address,
					Port = port
				};
				Subscribe(output);
				m_Clients[key] = output;
			}
			finally
			{
				m_ClientsSection.Leave();
			}

			ClientCallback handler = OnClientAdded;
			if (handler != null)
				handler(this, output);

			return output;
		}

		/// <summary>
		/// Removes the TCP client with the given key.
		/// </summary>
		/// <param name="key"></param>
		/// <returns>True if the client exists and was removed.</returns>
		private bool RemoveClient(string key)
		{
			AsyncTcpClient client = m_ClientsSection.Execute(() => m_Clients.GetDefault(key, null));
			return RemoveClient(client);
		}

		/// <summary>
		/// Generates a key for storing/retrieving TCP clients from the collection.
		/// </summary>
		/// <param name="address"></param>
		/// <param name="port"></param>
		/// <returns></returns>
		private static string GetKey(string address, ushort port)
		{
			return string.Format("{0}:{1}", address, port);
		}

		#endregion

		#region Client Callbacks

		/// <summary>
		/// Subscribe to the client events.
		/// </summary>
		/// <param name="client"></param>
		private void Subscribe(AsyncTcpClient client)
		{
			if (client == null)
				return;

			client.OnConnectedStateChanged += ClientOnConnectedStateChanged;
			client.OnSerialDataReceived += ClientOnSerialDataReceived;
		}

		/// <summary>
		/// Unsubscribe from the client events.
		/// </summary>
		/// <param name="client"></param>
		private void Unsubscribe(AsyncTcpClient client)
		{
			if (client == null)
				return;

			client.OnConnectedStateChanged -= ClientOnConnectedStateChanged;
			client.OnSerialDataReceived -= ClientOnSerialDataReceived;
		}

		/// <summary>
		/// Called when a client connects or disconnects.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void ClientOnConnectedStateChanged(object sender, BoolEventArgs args)
		{
			ClientConnectionStateCallback handler = OnClientConnectionStateChanged;
			if (handler != null)
				handler(this, sender as AsyncTcpClient, args.Data);
		}

		/// <summary>
		/// Called when a client receives data.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void ClientOnSerialDataReceived(object sender, StringEventArgs args)
		{
			ClientSerialDataCallback handler = OnClientSerialDataReceived;
			if (handler != null)
				handler(this, sender as AsyncTcpClient, args.Data);
		}

		#endregion
	}
}
