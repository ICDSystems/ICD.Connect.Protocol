﻿#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Network.Broadcast.Broadcasters;
using ICD.Connect.Protocol.Network.Utils;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.Network.EventArguments;
using ICD.Connect.Protocol.Network.Servers;

namespace ICD.Connect.Protocol.Network.Broadcast
{
	public sealed class BroadcastManager : IDisposable, IConsoleNode
	{
		private readonly IcdUdpServer m_UdpServer;
		private readonly JsonSerialBuffer m_Buffer;

		private readonly IcdHashSet<string> m_Addresses;
		private readonly SafeCriticalSection m_AddressesSection;

		private readonly int m_SystemId;

		private readonly Dictionary<Type, IBroadcaster> m_Broadcasters;
		private readonly Guid m_Session;

		public int SystemId { get { return m_SystemId; } }

		public Guid Session { get { return m_Session; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		public BroadcastManager()
			: this(0)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public BroadcastManager(int systemId)
		{
			m_Broadcasters = new Dictionary<Type, IBroadcaster>();
			m_SystemId = systemId;
			m_Session = Guid.NewGuid();

            m_UdpServer = new IcdUdpServer(NetworkUtils.GetBroadcastPortForSystem(m_SystemId), NetworkUtils.MULTICAST_ADDRESS);

			m_Addresses = new IcdHashSet<string>();
			m_AddressesSection = new SafeCriticalSection();

			Subscribe(m_UdpServer);

			m_Buffer = new JsonSerialBuffer();
			Subscribe(m_Buffer);
		}

		public void Dispose()
		{
			Unsubscribe(m_UdpServer);
			m_UdpServer.Dispose();

			Unsubscribe(m_Buffer);
			foreach (KeyValuePair<Type, IBroadcaster> broadcast in m_Broadcasters)
			{
				if (broadcast.Value != null)
					broadcast.Value.Dispose();
			}
		}

		#region Methods

		/// <summary>
		/// Starts broadcasting and listening for broadcasts.
		/// </summary>
		public void Start()
		{
			if (!m_UdpServer.IsListening)
				m_UdpServer.Start();
		}

		/// <summary>
		/// Stops broadcasting and listening for broadcasts.
		/// </summary>
		public void Stop()
		{
			if (m_UdpServer.IsListening)
				m_UdpServer.Stop();
		}

		/// <summary>
		/// Registers the recurring broadcaster with the manager.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="broadcaster"></param>
		public void RegisterBroadcaster<T>(IBroadcaster broadcaster)
		{
			if (broadcaster == null)
				throw new ArgumentNullException("broadcaster");

			broadcaster.SendBroadcastData = Broadcast;
			m_Broadcasters.Add(typeof(T), broadcaster);
		}

		/// <summary>
		/// Deregisters the recurring broadcaster with the manager.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="broadcaster"></param>
		public void DeregisterBroadcaster<T>(IBroadcaster broadcaster)
		{
			if (broadcaster == null)
				throw new ArgumentNullException("broadcaster");

			broadcaster.SendBroadcastData = null;
			m_Broadcasters.Remove(typeof(T));
		}

		/// <summary>
		/// Multicast does not work between subnets, so we can manually add known addresses to be advertised to.
		/// </summary>
		/// <param name="address"></param>
		[PublicAPI]
		public void AddBroadcastAddress(string address)
		{
			m_AddressesSection.Execute(() => m_Addresses.Add(address));
		}

		/// <summary>
		/// Multicast does not work between subnets, so we can manually add known addresses to be advertised to.
		/// </summary>
		/// <param name="addresses"></param>
		[PublicAPI]
		public void AddBroadcastAddresses(IEnumerable<string> addresses)
		{
			foreach (string address in addresses)
				AddBroadcastAddress(address);
		}

		/// <summary>
		/// Multicast does not work between subnets, so we can manually set known addresses to be advertised to.
		/// </summary>
		/// <param name="addresses"></param>
		[PublicAPI]
		public void SetBroadcastAddresses(IEnumerable<string> addresses)
		{
			if (addresses == null)
				throw new ArgumentNullException("addresses");

			m_AddressesSection.Enter();

			try
			{
				m_Addresses.Clear();
				m_Addresses.AddRange(addresses);
			}
			finally
			{
				m_AddressesSection.Leave();
			}
		}

		/// <summary>
		/// Gets the known multicast addresses.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<string> GetBroadcastAddresses()
		{
			return m_AddressesSection.Execute(() => m_Addresses.ToArray(m_Addresses.Count));
		}

		/// <summary>
		/// Removes manually added advertisement addresses.
		/// </summary>
		/// <param name="address"></param>
		[PublicAPI]
		public void RemoveBroadcastAddress(string address)
		{
			m_AddressesSection.Execute(() => m_Addresses.Remove(address));
		}

		/// <summary>
		/// Gets the address of the advertisement manager.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public HostInfo GetHostInfo()
		{
			string address = IcdEnvironment.NetworkAddresses.FirstOrDefault();
			ushort port = NetworkUtils.GetBroadcastPortForSystem(m_SystemId);

			return new HostInfo(address, port);
		}

		public HostSessionInfo GetHostSessionInfo()
		{
			HostInfo host = GetHostInfo();
			return new HostSessionInfo(host, Session);
		}

		#endregion

		/// <summary>
		/// Broadcast data to the network.
		/// </summary>
		private void Broadcast(object data)
		{
			if (data == null)
				return;

			if (!m_UdpServer.IsListening)
				return;

			BroadcastData broadcastData = new BroadcastData {HostSession = GetHostSessionInfo()};
			broadcastData.SetData<object>(data);

			string serial = broadcastData.Serialize();

			// Loop over the ports for the different program slots
			string[] addresses = GetAdvertisementAddresses().ToArray();
			ushort[] ports = GetAdvertisementPorts().ToArray();

			foreach (string address in addresses)
			{
				foreach (ushort port in ports)
					m_UdpServer.Send(serial, address, port);
			}
		}

		#region Protected Methods

		/// <summary>
		/// Gets all of the addresses each advertisement is broadcast to.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<string> GetAdvertisementAddresses()
		{
			// Todo - workaround for "yield" causing threading issue at build
			List<string> output = new List<string> {NetworkUtils.LOCALHOST_ADDRESS, NetworkUtils.MULTICAST_ADDRESS};

			// Broadcast to program specified addresses
			string[] addresses = m_AddressesSection.Execute(() => m_Addresses.ToArray(m_Addresses.Count));
			output.AddRange(addresses);

			return output;
		}

		/// <summary>
		/// Gets all of the ports each advertisement is broadcast to.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<ushort> GetAdvertisementPorts()
		{
			return NetworkUtils.GetBroadcastPorts(m_SystemId);
		}

		#endregion

		#region UDP Client Callbacks

		/// <summary>
		/// Subscribe to the UDP Client events.
		/// </summary>
		/// <param name="udpServer"></param>
		private void Subscribe(IcdUdpServer udpServer)
		{
			udpServer.OnDataReceived += UdpServerOnDataReceived;
			udpServer.OnIsListeningStateChanged += UdpServerOnIsListeningStateChanged;
		}

		/// <summary>
		/// Unsubscribe from the UDP Client events.
		/// </summary>
		/// <param name="udpServer"></param>
		private void Unsubscribe(IcdUdpServer udpServer)
		{
			udpServer.OnDataReceived -= UdpServerOnDataReceived;
			udpServer.OnIsListeningStateChanged -= UdpServerOnIsListeningStateChanged;
		}

		/// <summary>
		/// Called when the UDP Server receives some data.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void UdpServerOnDataReceived(object sender, UdpDataReceivedEventArgs args)
		{
			m_Buffer.Enqueue(args.Data);
		}

		/// <summary>
		/// Called when the UDP Client connects/disconnects.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void UdpServerOnIsListeningStateChanged(object sender, BoolEventArgs args)
		{
			m_Buffer.Clear();
		}

		#endregion

		#region Buffer Callbacks

		/// <summary>
		/// Subscribe to the buffer events.
		/// </summary>
		/// <param name="buffer"></param>
		private void Subscribe(JsonSerialBuffer buffer)
		{
			buffer.OnCompletedSerial += BufferOnCompletedSerial;
		}

		/// <summary>
		/// Unsubscribe from the buffer events.
		/// </summary>
		/// <param name="buffer"></param>
		private void Unsubscribe(JsonSerialBuffer buffer)
		{
			buffer.OnCompletedSerial -= BufferOnCompletedSerial;
		}

		/// <summary>
		/// Called when the buffer raises complete serial data.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void BufferOnCompletedSerial(object sender, StringEventArgs args)
		{
			BroadcastData broadcastData;

			try
			{
				broadcastData = JsonConvert.DeserializeObject<BroadcastData>(args.Data);
			}
			catch (Exception e)
			{
				ServiceProvider.TryGetService<ILoggerService>().AddEntry(eSeverity.Error, "BroadcastData JsonException:{0} - Data:{1}", e.GetType(), args.Data);
				if (e is JsonReaderException || e is JsonSerializationException)
				{
					ServiceProvider.TryGetService<ILoggerService>()
					               .AddEntry(eSeverity.Error, "{0} Failed to deserialize broadcast - {1}", GetType().Name, e.Message);
					return;
				}
				
				throw;
			}

			// Broadcast back to the place we got this advertisement from
			AddBroadcastAddress(broadcastData.HostSession.Host.Address);

			Type type = broadcastData.Type;
			if (type != null && m_Broadcasters.ContainsKey(type))
				m_Broadcasters[type].HandleIncomingBroadcast(broadcastData);
		}

		#endregion

		#region Console

		public string ConsoleName { get { return "BroadcastManager"; } }

		public string ConsoleHelp
		{
			get { return "Manages multicast and directed broadcasts for any registered serializable data"; }
		}

		public IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{

			yield return m_UdpServer;

			IEnumerable<IConsoleNodeBase> broadcasters =
				m_Broadcasters.OrderBy(kvp => kvp.Key.Name)
				              .Select(kvp => kvp.Value)
				              .OfType<IConsoleNodeBase>();

			yield return ConsoleNodeGroup.IndexNodeMap("Broadcasters", "", broadcasters);
		}

		public void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("System ID", m_SystemId);
			addRow("Host Session Info", GetHostSessionInfo());
			addRow("Active", m_UdpServer.IsListening);
			addRow("Addresses", StringUtils.ArrayFormat(GetAdvertisementAddresses().Order()));
		}

		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new ConsoleCommand("Start", "Resumes broadcasting and accepting broadcasts", () => Start());
			yield return new ConsoleCommand("Stop", "Stops broadcasting and accepting broadcasts", () => Stop());

		    yield return
		        new GenericConsoleCommand<string>("AddAddress", "Adds an address to the broadcast list",
		                                          a => AddBroadcastAddress(a));
		    yield return
		        new GenericConsoleCommand<string>("RemoveAddress", "Removes an address from the broadcast list",
		                                          a => RemoveBroadcastAddress(a));

		    yield return new ConsoleCommand("PrintBroadcasters", "Prints a table of the registered broadcasters",
		                                    () => PrintBroadcasters());
		}

		private string PrintBroadcasters()
		{
			TableBuilder builder = new TableBuilder("Type", "Broadcaster");

			foreach (var kvp in m_Broadcasters.OrderBy(k => k.Key.Name))
				builder.AddRow(kvp.Key.Name, kvp.Value);

			return builder.ToString();
		}

		#endregion
	}
}
