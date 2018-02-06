﻿using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Network.Udp;
using ICD.Connect.Protocol.Network.Utils;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;
using Newtonsoft.Json;

namespace ICD.Connect.Protocol.Network.Broadcast
{
	public sealed class BroadcastManager : IDisposable, IConsoleNode
	{
		private readonly AsyncUdpClient m_UdpClient;
		private readonly JsonSerialBuffer m_Buffer;

		private readonly IcdHashSet<string> m_Addresses;
		private readonly SafeCriticalSection m_AddressesSection;

		private readonly int m_SystemId;

		private readonly Dictionary<Type, RecurringBroadcaster> m_Broadcasters;

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
			m_Broadcasters = new Dictionary<Type, RecurringBroadcaster>();
			m_SystemId = systemId;

			m_UdpClient = new AsyncUdpClient
			{
				Address = NetworkUtils.MULTICAST_ADDRESS,
				Port = NetworkUtils.GetBroadcastPortForSystem(m_SystemId),
			};

			m_Addresses = new IcdHashSet<string>();
			m_AddressesSection = new SafeCriticalSection();

			Subscribe(m_UdpClient);

			m_Buffer = new JsonSerialBuffer();
			Subscribe(m_Buffer);

			m_UdpClient.Connect();
		}

		public void Dispose()
		{
			Unsubscribe(m_UdpClient);
			m_UdpClient.Dispose();

			Unsubscribe(m_Buffer);
			foreach (KeyValuePair<Type, RecurringBroadcaster> broadcast in m_Broadcasters)
			{
				if (broadcast.Value != null)
					broadcast.Value.Dispose();
			}
		}

		#region Methods

		/// <summary>
		/// Registers the recurring broadcaster with the manager.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="broadcaster"></param>
		public void RegisterBroadcaster<T>(RecurringBroadcaster<T> broadcaster)
		{
			broadcaster.SendBroadcastData = Broadcast;
			m_Broadcasters.Add(typeof(T), broadcaster);
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

		#endregion

		/// <summary>
		/// Broadcast data to the network.
		/// </summary>
		private void Broadcast(object data)
		{
			BroadcastData broadcastData = new BroadcastData(GetHostInfo(), data);
			string serial = broadcastData.Serialize();

			// Loop over the ports for the different program slots
			string[] addresses = GetAdvertisementAddresses().ToArray();
			ushort[] ports = GetAdvertisementPorts().ToArray();

			foreach (string address in addresses)
			{
				foreach (ushort port in ports)
					m_UdpClient.SendToAddress(serial, address, port);
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
			string[] addresses = m_AddressesSection.Execute(() => m_Addresses.ToArray());
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
		/// <param name="udpClient"></param>
		private void Subscribe(AsyncUdpClient udpClient)
		{
			udpClient.OnSerialDataReceived += UdpClientOnSerialDataReceived;
		}

		/// <summary>
		/// Unsubscribe from the UDP Client events.
		/// </summary>
		/// <param name="udpClient"></param>
		private void Unsubscribe(AsyncUdpClient udpClient)
		{
			udpClient.OnSerialDataReceived -= UdpClientOnSerialDataReceived;
		}

		/// <summary>
		/// Called when the UDP Client receives some data.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void UdpClientOnSerialDataReceived(object sender, StringEventArgs args)
		{
			m_Buffer.Enqueue(args.Data);
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
				broadcastData = BroadcastData.Deserialize(args.Data);
			}
			catch (JsonReaderException e)
			{
				ServiceProvider.TryGetService<ILoggerService>()
				               .AddEntry(eSeverity.Error, "{0} Failed to deserialize broadcast - {1}", GetType().Name, e.Message);
				return;
			}

			// Broadcast back to the place we got this advertisement from
			AddBroadcastAddress(broadcastData.Source.Address);

			Type type = Type.GetType(broadcastData.Type);
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
			return m_Broadcasters.Values.Cast<IConsoleNodeBase>();
		}

		public void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("Broadcasts", m_Broadcasters.Values.Count);
			addRow("Address list", GetAdvertisementAddresses().Count());
		}

		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return
				new GenericConsoleCommand<string>("AddAddress", "Adds an address to the broadcast list", a => AddBroadcastAddress(a))
				;
		}

		#endregion
	}
}
