﻿using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Timers;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.Network.Ports.Tcp
{
	/// <summary>
	/// The TcpClientPool provides a way to share existing TCP clients.
	/// </summary>
	public sealed class TcpClientPool : IDisposable, IConsoleNode
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

		private readonly Dictionary<HostInfo, AsyncTcpClient> m_Clients;
		private readonly Dictionary<AsyncTcpClient, SafeTimer> m_ClientDisposalTimers;
		private readonly SafeCriticalSection m_ClientsSection;

		private eDebugMode m_DebugRx;
		private eDebugMode m_DebugTx;

		#region Properties

		/// <summary>
		/// Gets the number of TCP clients.
		/// </summary>
		public int Count { get { return m_ClientsSection.Execute(() => m_Clients.Count); } }

		/// <summary>
		/// When enabled prints the received data to the console.
		/// </summary>
		[PublicAPI]
		public eDebugMode DebugRx
		{
			get { return m_ClientsSection.Execute(() => m_DebugRx); }
			set
			{
				m_ClientsSection.Enter();

				try
				{
					if (value == m_DebugRx)
						return;

					m_DebugRx = value;

					foreach (AsyncTcpClient client in m_Clients.Values)
						client.DebugRx = m_DebugRx;
				}
				finally
				{
					m_ClientsSection.Leave();
				}
			}
		}

		/// <summary>
		/// When enabled prints the transmitted data to the console.
		/// </summary>
		[PublicAPI]
		public eDebugMode DebugTx
		{
			get { return m_ClientsSection.Execute(() => m_DebugTx); }
			set
			{
				m_ClientsSection.Enter();

				try
				{
					if (value == m_DebugTx)
						return;

					m_DebugTx = value;

					foreach (AsyncTcpClient client in m_Clients.Values)
						client.DebugTx = m_DebugTx;
				}
				finally
				{
					m_ClientsSection.Leave();
				}
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public TcpClientPool()
		{
			m_Clients = new Dictionary<HostInfo, AsyncTcpClient>();
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
			m_ClientsSection.Enter();

			try
			{
				HostInfo[] keys = m_Clients.Keys.ToArray();
				foreach (HostInfo key in keys)
					RemoveClient(key);
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
			m_ClientsSection.Enter();

			try
			{
				AsyncTcpClient client = LazyLoadClient(host);
				StopDisposeTimer(client);
				return client;
			}
			finally
			{
				m_ClientsSection.Leave();
			}
		}

		/// <summary>
		/// Removes the TCP client from the pool.
		/// </summary>
		/// <param name="client"></param>
		/// <returns>True if the client exists and was removed.</returns>
		[PublicAPI]
		public bool RemoveClient(AsyncTcpClient client)
		{
			if (client == null)
				throw new ArgumentNullException("client");

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
		private void DisposeClient(AsyncTcpClient client)
		{
			if (client == null)
				throw new ArgumentNullException("client");

			RemoveClient(client);
			client.Dispose();
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
			if (client == null)
				throw new ArgumentNullException("client");

			m_ClientsSection.Enter();

			try
			{
				SafeTimer timer;
				if (!m_ClientDisposalTimers.TryGetValue(client, out timer))
				{
					timer = SafeTimer.Stopped(() => DisposeClient(client));
					m_ClientDisposalTimers.Add(client, timer);
				}

				timer.Reset(keepAlive);
			}
			finally
			{
				m_ClientsSection.Leave();
			}
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// If the client is currently scheduled for disposal we end the timer.
		/// </summary>
		/// <param name="client"></param>
		private void StopDisposeTimer(AsyncTcpClient client)
		{
			m_ClientsSection.Enter();

			try
			{
				SafeTimer timer;
				if (!m_ClientDisposalTimers.TryGetValue(client, out timer))
					return;

				timer.Dispose();
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
		/// <param name="host"></param>
		/// <returns></returns>
		private AsyncTcpClient LazyLoadClient(HostInfo host)
		{
			AsyncTcpClient output;

			m_ClientsSection.Enter();

			try
			{
				if (m_Clients.TryGetValue(host, out output))
					return output;

				// Instantiate the new client
				output = new AsyncTcpClient
				{
					Name = GetType().Name,
					Address = host.Address,
					Port = host.Port
				};
				Subscribe(output);
				m_Clients.Add(host, output);
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
		private void RemoveClient(HostInfo key)
		{
			m_ClientsSection.Enter();

			try
			{
				AsyncTcpClient client;
				if (m_Clients.TryGetValue(key, out client))
					RemoveClient(client);
			}
			finally
			{
				m_ClientsSection.Leave();
			}
		}

		#endregion

		#region Client Callbacks

		/// <summary>
		/// Subscribe to the client events.
		/// </summary>
		/// <param name="client"></param>
		private void Subscribe(AsyncTcpClient client)
		{
			client.OnConnectedStateChanged += ClientOnConnectedStateChanged;
			client.OnSerialDataReceived += ClientOnSerialDataReceived;
		}

		/// <summary>
		/// Unsubscribe from the client events.
		/// </summary>
		/// <param name="client"></param>
		private void Unsubscribe(AsyncTcpClient client)
		{
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

		#region Console

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public string ConsoleName { get { return GetType().Name; } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public string ConsoleHelp { get { return string.Empty; } }

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield break;
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("Count", Count);
			addRow("Debug Rx", DebugRx);
			addRow("Debug Tx", DebugTx);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new ConsoleCommand("EnableDebug", "Sets debug mode for TX/RX to Ascii",
			                                () =>
			                                {
				                                SetTxDebugMode(eDebugMode.Ascii);
				                                SetRxDebugMode(eDebugMode.Ascii);
			                                });

			yield return new ConsoleCommand("DisableDebug", "Sets debug mode for TX/RX to Off",
			                                () =>
			                                {
				                                SetTxDebugMode(eDebugMode.Off);
				                                SetRxDebugMode(eDebugMode.Off);
			                                });

			yield return new EnumConsoleCommand<eDebugMode>("SetDebugMode",
			                                                p =>
			                                                {
				                                                SetTxDebugMode(p);
				                                                SetRxDebugMode(p);
			                                                });

			yield return new EnumConsoleCommand<eDebugMode>("SetDebugModeTx", p => SetTxDebugMode(p));
			yield return new EnumConsoleCommand<eDebugMode>("SetDebugModeRx", p => SetRxDebugMode(p));

			yield return new ConsoleCommand("PrintClients", "Prints a table of the pooled TCP clients", () => PrintClients());
		}

		private string PrintClients()
		{
			TableBuilder builder = new TableBuilder("Host", "Client");

			m_ClientsSection.Enter();

			try
			{
				foreach (KeyValuePair<HostInfo, AsyncTcpClient> kvp in m_Clients)
					builder.AddRow(kvp.Key, kvp.Value);
			}
			finally
			{
				m_ClientsSection.Leave();
			}

			return builder.ToString();
		}

		private void SetTxDebugMode(eDebugMode mode)
		{
			DebugTx = mode;
		}

		private void SetRxDebugMode(eDebugMode mode)
		{
			DebugRx = mode;
		}

		#endregion
	}
}
