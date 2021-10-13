#if NETFRAMEWORK
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
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Network.EventArguments;
using ICD.Connect.Protocol.Network.Servers;
using ICD.Connect.Protocol.Crosspoints.CrosspointManagers;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;

namespace ICD.Connect.Protocol.Crosspoints.Advertisements
{
	/// <summary>
	/// The AdvertisementManager is responsible for broadcasting the local crosspoints,
	/// and discovering remote crosspoints.
	/// </summary>
	public sealed class AdvertisementManager : IDisposable, IConsoleNode
	{
		/// <summary>
		/// How often to broadcast the available crosspoints.
		/// </summary>
		public const long BROADCAST_INTERVAL = 30 * 1000;

		/// <summary>
		/// Raised when crosspoints are discovered.
		/// </summary>
		public event EventHandler<AdvertisementEventArgs> OnCrosspointsDiscovered;

		/// <summary>
		/// Raised when crosspoints are removed.
		/// </summary>
		public event EventHandler<AdvertisementEventArgs> OnCrosspointsRemoved;

		private readonly Dictionary<string, eAdvertisementType> m_Addresses;
		private readonly SafeCriticalSection m_AddressesSection;

		private readonly IcdUdpServer m_UdpServer;
		private readonly JsonSerialBuffer m_Buffer;

		private readonly SafeTimer m_BroadcastTimer;
		private readonly int m_SystemId;

		private EquipmentCrosspointManager m_EquipmentManager;
		private ControlCrosspointManager m_ControlManager;

		#region Properties

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public string ConsoleName { get { return GetType().Name; } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public string ConsoleHelp { get { return "Responsible for broadcasting local crosspoints to the network."; } }

		private ILoggerService Logger { get { return ServiceProvider.TryGetService<ILoggerService>(); } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="systemId"></param>
		public AdvertisementManager(int systemId)
		{
			m_Addresses = new Dictionary<string, eAdvertisementType>
			{
				{
					// Broadcast to ourself and other programs on the same processor.
					Xp3Utils.LOCALHOST_ADDRESS,
					eAdvertisementType.Localhost
				},
				{
					// Broadcast to processors on the current subnet (does not include localhost)
					Xp3Utils.MULTICAST_ADDRESS,
					eAdvertisementType.Multicast
				}
			};
			m_AddressesSection = new SafeCriticalSection();

			m_SystemId = systemId;

			m_UdpServer = new IcdUdpServer(Xp3Utils.GetPortForSystem(systemId));

			Subscribe(m_UdpServer);

			m_Buffer = new JsonSerialBuffer();
			Subscribe(m_Buffer);

			m_BroadcastTimer = new SafeTimer(TryBroadcast, BROADCAST_INTERVAL, BROADCAST_INTERVAL);

			m_UdpServer.Start();
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			OnCrosspointsDiscovered = null;

			m_BroadcastTimer.Dispose();

			m_UdpServer.Dispose();
			Unsubscribe(m_UdpServer);

			Unsubscribe(m_Buffer);

			StopAdvertisingControlCrosspoints();
			StopAdvertisingEquipmentCrosspoints();
		}

		/// <summary>
		/// Broadcast the available controls to the network.
		/// </summary>
		public void Broadcast()
		{
			CrosspointInfo[] controls = GetCrosspointInfo(m_ControlManager).ToArray();
			CrosspointInfo[] equipment = GetCrosspointInfo(m_EquipmentManager).ToArray();

			// Don't bother advertising if we don't have any crosspoints.
			if (controls.Length == 0 && equipment.Length == 0)
				return;

			Advertisement advertisement = new Advertisement
			{
				Source = GetHostInfo(),
				Controls = controls,
				Equipment = equipment
			};

			// Loop over the addressest to advertise to
			foreach (KeyValuePair<string, eAdvertisementType> address in GetAdvertisementAddresses())
			{
				advertisement.AdvertisementType = address.Value;
				string serial = advertisement.Serialize();

				// Loop over the ports for the different program slots
				foreach (ushort port in GetAdvertisementPorts())
					m_UdpServer.Send(serial, address.Key, port);
			}
		}

		/// <summary>
		/// Sends a directed remove advertisement to the specified address
		/// </summary>
		/// <param name="address"></param>
		private void SendRemoveDirected(string address)
		{
			CrosspointInfo[] controls = GetCrosspointInfo(m_ControlManager).ToArray();
			CrosspointInfo[] equipment = GetCrosspointInfo(m_EquipmentManager).ToArray();

			// Don't bother advertising if we don't have any crosspoints.
			if (controls.Length == 0 && equipment.Length == 0)
				return;

			Advertisement advertisement = new Advertisement
			{
				Source = GetHostInfo(),
				Controls = controls,
				Equipment = equipment,
				AdvertisementType = eAdvertisementType.DirectedRemove
			};

			string serial = advertisement.Serialize();

			foreach (ushort port in GetAdvertisementPorts())
				m_UdpServer.Send(serial, address, port);
		}

		/// <summary>
		/// Gets the address of the advertisement manager.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public HostInfo GetHostInfo()
		{
			string address = IcdEnvironment.NetworkAddresses.FirstOrDefault();
			ushort port = Xp3Utils.GetPortForSystem(m_SystemId);

			return new HostInfo(address, port);
		}

		/// <summary>
		/// Advertises the control crosspoints for remote discovery.
		/// </summary>
		/// <param name="crosspointManager"></param>
		[PublicAPI]
		public void AdvertiseControlCrosspoints(ControlCrosspointManager crosspointManager)
		{
			if (crosspointManager == m_ControlManager)
				return;

			StopAdvertisingControlCrosspoints();

			m_ControlManager = crosspointManager;
			Subscribe(m_ControlManager);

			Broadcast();
		}

		/// <summary>
		/// Advertises the equipment crosspoints for remote discovery.
		/// </summary>
		/// <param name="crosspointManager"></param>
		[PublicAPI]
		public void AdvertiseEquipmentCrosspoints(EquipmentCrosspointManager crosspointManager)
		{
			if (crosspointManager == m_EquipmentManager)
				return;

			StopAdvertisingEquipmentCrosspoints();

			m_EquipmentManager = crosspointManager;
			Subscribe(m_EquipmentManager);

			Broadcast();
		}

		/// <summary>
		/// Stops advertising control crosspoints for remote discovery.
		/// </summary>
		[PublicAPI]
		public void StopAdvertisingControlCrosspoints()
		{
			Unsubscribe(m_ControlManager);
			m_ControlManager = null;
		}

		/// <summary>
		/// Stops advertising equipment crosspoints for remote discovery.
		/// </summary>
		[PublicAPI]
		public void StopAdvertisingEquipmentCrosspoints()
		{
			Unsubscribe(m_EquipmentManager);
			m_EquipmentManager = null;
		}

		/// <summary>
		/// Multicast does not work between subnets, so we can manually add known addresses to be advertised to.
		/// </summary>
		/// <param name="address"></param>
		/// <param name="advertisementType"></param>
		[PublicAPI]
		public void AddAdvertisementAddress(string address, eAdvertisementType advertisementType)
		{
			m_AddressesSection.Enter();

			try
			{
				if (m_Addresses.ContainsKey(address))
					return;
				m_Addresses.Add(address, advertisementType);
			}
			finally
			{
				m_AddressesSection.Leave();
			}

			Broadcast();
		}

		/// <summary>
		/// Multicast does not work between subnets, so we can manually add known addresses to be advertised to.
		/// </summary>
		/// <param name="address"></param>
		[PublicAPI]
		public void AddDirectedAdvertisementAddress(string address)
		{
			m_AddressesSection.Enter();

			try
			{
				if (m_Addresses.ContainsKey(address))
					return;
				m_Addresses.Add(address, eAdvertisementType.Directed);
			}
			finally
			{
				m_AddressesSection.Leave();
			}

			Broadcast();
		}

		/// <summary>
		/// Multicast does not work between subnets, so we can manually add known addresses to be advertised to.
		/// </summary>
		/// <param name="addresses"></param>
		[PublicAPI]
		public void AddDirectedAdvertisementAddresses(IEnumerable<string> addresses)
		{
			foreach (string address in addresses)
				AddDirectedAdvertisementAddress(address);
		}

		/// <summary>
		/// Removes manually added advertisement addresses.
		/// </summary>
		/// <param name="address"></param>
		[PublicAPI]
		public void RemoveAdvertisementAddress(string address)
		{
			bool isDirected = false;

			m_AddressesSection.Enter();

			try
			{
				eAdvertisementType advertisementType;
				if (m_Addresses.TryGetValue(address, out advertisementType))
				{
					isDirected = advertisementType == eAdvertisementType.Directed;
					m_Addresses.Remove(address);
				}
			}
			finally
			{
				m_AddressesSection.Leave();
			}

			//If we removed a directed address, send an advertisement to the remote host to remove us also

			if (isDirected)
				SendRemoveDirected(address);
		}

		public override string ToString()
		{
			return new ReprBuilder(this).AppendProperty("SystemId", m_SystemId).ToString();
		}

		#endregion

		#region Private Methods

		private void TryBroadcast()
		{
			if (!m_UdpServer.IsDisposed)
				Broadcast();
		}

		/// <summary>
		/// Gets all of the addresses each advertisement is broadcast to.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<KeyValuePair<string, eAdvertisementType>> GetAdvertisementAddresses()
		{
			return m_AddressesSection.Execute(() => m_Addresses.ToArray());
		}

		/// <summary>
		/// Gets all of the ports each advertisement is broadcast to.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<ushort> GetAdvertisementPorts()
		{
			return Xp3Utils.GetAdvertisementMulticastPorts(m_SystemId);
		}

		/// <summary>
		/// Gets the info for the crosspoints of the given manager.
		/// </summary>
		/// <param name="crosspointManager"></param>
		/// <returns></returns>
		private static IEnumerable<CrosspointInfo> GetCrosspointInfo(ICrosspointManager crosspointManager)
		{
			if (crosspointManager == null)
				return Enumerable.Empty<CrosspointInfo>();

			HostInfo host = crosspointManager.GetHostInfo();

			return crosspointManager.GetCrosspoints().Select(c => new CrosspointInfo(c.Id, c.Name, host));
		}

		#endregion

		#region Crosspoint Manager Callbacks

		/// <summary>
		/// Subscribe to the crosspoint manager events.
		/// </summary>
		/// <param name="crosspointManager"></param>
		private void Subscribe(ICrosspointManager crosspointManager)
		{
			if (crosspointManager == null)
				return;

			crosspointManager.OnCrosspointRegistered += CrosspointManagerOnCrosspointRegistered;
			crosspointManager.OnCrosspointUnregistered += CrosspointManagerOnCrosspointUnregistered;
		}

		/// <summary>
		/// Unsubscribe from the crosspoint manager events.
		/// </summary>
		/// <param name="crosspointManager"></param>
		private void Unsubscribe(ICrosspointManager crosspointManager)
		{
			if (crosspointManager == null)
				return;

			crosspointManager.OnCrosspointRegistered -= CrosspointManagerOnCrosspointRegistered;
			crosspointManager.OnCrosspointUnregistered -= CrosspointManagerOnCrosspointUnregistered;
		}

		/// <summary>
		/// Called when a CrosspointManager registers a crosspoint.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="crosspoint"></param>
		private void CrosspointManagerOnCrosspointRegistered(ICrosspointManager sender, ICrosspoint crosspoint)
		{
			Broadcast();
		}

		/// <summary>
		/// Called when a CrosspointManager unregisteres a crosspoint.
		/// Sends an advertisement to remove the crosspoint immediately from all neighbors.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="crosspoint"></param>
		private void CrosspointManagerOnCrosspointUnregistered(ICrosspointManager sender, ICrosspoint crosspoint)
		{
			CrosspointInfo crosspointInfo = new CrosspointInfo(crosspoint.Id, crosspoint.Name, GetHostInfo());

			CrosspointInfo[] controls = sender == m_ControlManager ? new[] {crosspointInfo} : new CrosspointInfo[0];
			CrosspointInfo[] equipment = sender == m_EquipmentManager ? new[] {crosspointInfo} : new CrosspointInfo[0];

			// Don't bother advertising if we don't have any crosspoints.
			if (controls.Length == 0 && equipment.Length == 0)
				return;

			Advertisement advertisement = new Advertisement
			{
				Source = GetHostInfo(),
				Controls = controls,
				Equipment = equipment,
				AdvertisementType = eAdvertisementType.CrosspointRemove
			};

			string serial = advertisement.Serialize();

			// Loop over the addresses to advertise to
			foreach (KeyValuePair<string, eAdvertisementType> address in GetAdvertisementAddresses())
			{
				// Loop over the ports for the different program slots
				foreach (ushort port in GetAdvertisementPorts())
					m_UdpServer.Send(serial, address.Key, port);
			}
		}

		#endregion

		#region UDP Server Callbacks

		/// <summary>
		/// Subscribe to the UDP Client events.
		/// </summary>
		/// <param name="udpServer"></param>
		private void Subscribe(IcdUdpServer udpServer)
		{
			udpServer.OnDataReceived += UdpClientOnSerialDataReceived;
		}

		/// <summary>
		/// Unsubscribe from the UDP Client events.
		/// </summary>
		/// <param name="udpServer"></param>
		private void Unsubscribe(IcdUdpServer udpServer)
		{
			udpServer.OnDataReceived -= UdpClientOnSerialDataReceived;
		}

		/// <summary>
		/// Called when the UDP Client receives some data.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void UdpClientOnSerialDataReceived(object sender, UdpDataReceivedEventArgs args)
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
			Advertisement advertisement;

			try
			{
				advertisement = Advertisement.Deserialize(args.Data);
			}
			catch (JsonSerializationException e)
			{
				Logger.AddEntry(eSeverity.Error, e, "{0} - Exception deserializing advertisement", this);
				return;
			}

			// Broadcast back to the place we got this advertisement from
			switch (advertisement.AdvertisementType)
			{
				case eAdvertisementType.Directed:
				{
					string address = advertisement.Source.AddressOrLocalhost;
					AddAdvertisementAddress(address, eAdvertisementType.Directed);
					break;
				}

				// The below cases are all remove types, which should all goto CrosspointRemove,
				// to notify the managers that the received crosspoints should be removed from
				// their remote crosspoints list.
				case eAdvertisementType.DirectedRemove:
				{
					RemoveAdvertisementAddress(advertisement.Source.Address);
					goto case eAdvertisementType.CrosspointRemove;
				}
				case eAdvertisementType.MeshRemove:
				{
					// If/when Mesh is implemented, add mesh remove commands here (if any)
					goto case eAdvertisementType.CrosspointRemove;
				}
				case eAdvertisementType.CrosspointRemove:
				{
					OnCrosspointsRemoved.Raise(this, new AdvertisementEventArgs(advertisement));
					return;
					//break;
				}
			}

			OnCrosspointsDiscovered.Raise(this, new AdvertisementEventArgs(advertisement));
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield return m_UdpServer;
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("System Id", m_SystemId);
			addRow("Has Control Crosspoint Manager", m_ControlManager != null);
			addRow("Has Equipment Crosspoint Manager", m_EquipmentManager != null);
			addRow("Broadcast Interval (seconds)", BROADCAST_INTERVAL / 1000.0f);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new GenericConsoleCommand<string>("AddAddress", "Adds the address to the list of broadcast destinations with type Directed", s => AddAdvertisementAddress(s, eAdvertisementType.Directed));
			yield return
				new GenericConsoleCommand<string>("RemoveAddress", "Removes the address from the list of broadcast destinations",
				                                  s => RemoveAdvertisementAddress(s));
			yield return new ConsoleCommand("Broadcast", "Immediately broadcasts the local crosspoints to the network", () => Broadcast());
			yield return new ConsoleCommand("PrintAddresses", "Prints the addresses that advertisements are being sent to", () => PrintAddresses());
		}

		private string PrintAddresses()
		{
			IEnumerable<string> addresses =
				GetAdvertisementAddresses().Select(kvp => string.Format("{0} ({1})", kvp.Key, kvp.Value));

			return string.Format("Addresses: {0}{1}Ports: {2}",
			                     StringUtils.ArrayFormat(addresses),
			                     IcdEnvironment.NewLine,
			                     StringUtils.ArrayRangeFormat(GetAdvertisementPorts()));
		}

		#endregion
	}
}
