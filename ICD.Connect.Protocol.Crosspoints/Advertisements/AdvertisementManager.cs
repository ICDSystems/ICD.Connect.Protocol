using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Common.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Timers;
using ICD.Connect.Protocol.Network.Udp;
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

		private readonly IcdHashSet<string> m_Addresses;
		private readonly SafeCriticalSection m_AddressesSection;

		private readonly AsyncUdpClient m_UdpClient;
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

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="systemId"></param>
		public AdvertisementManager(int systemId)
		{
			m_Addresses = new IcdHashSet<string>
			{
				// Broadcast to ourself and other programs on the same processor.
				Xp3Utils.LOCALHOST_ADDRESS,

				// Broadcast to processors on the current subnet (does not include localhost)
				Xp3Utils.MULTICAST_ADDRESS
			};
			m_AddressesSection = new SafeCriticalSection();

			m_SystemId = systemId;

			m_UdpClient = new AsyncUdpClient
			{
				Address = Xp3Utils.MULTICAST_ADDRESS,
				Port = Xp3Utils.GetPortForSystem(systemId),
			};

			Subscribe(m_UdpClient);

			m_Buffer = new JsonSerialBuffer();
			Subscribe(m_Buffer);

			m_BroadcastTimer = new SafeTimer(Broadcast, BROADCAST_INTERVAL, BROADCAST_INTERVAL);

			m_UdpClient.Connect();
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			OnCrosspointsDiscovered = null;

			m_BroadcastTimer.Dispose();

			m_UdpClient.Dispose();
			Unsubscribe(m_UdpClient);

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

			Advertisement advertisement = new Advertisement(GetHostInfo(), controls, equipment);
			string serial = advertisement.Serialize();

			// Loop over the ports for the different program slots
			foreach (string address in GetAdvertisementAddresses())
			{
				foreach (ushort port in GetAdvertisementPorts())
					m_UdpClient.SendToAddress(serial, address, port);
			}
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
		[PublicAPI]
		public void AddAdvertisementAddress(string address)
		{
			m_AddressesSection.Enter();

			try
			{
				if (!m_Addresses.Add(address))
					return;
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
		public void AddAdvertisementAddresses(IEnumerable<string> addresses)
		{
			foreach (string address in addresses)
				AddAdvertisementAddress(address);
		}

		/// <summary>
		/// Removes manually added advertisement addresses.
		/// </summary>
		/// <param name="address"></param>
		[PublicAPI]
		public void RemoveAdvertisementAddress(string address)
		{
			m_AddressesSection.Execute(() => m_Addresses.Remove(address));
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets all of the addresses each advertisement is broadcast to.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<string> GetAdvertisementAddresses()
		{
			return m_AddressesSection.Execute(() => m_Addresses.Order().ToArray());
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
			Advertisement advertisement = Advertisement.Deserialize(args.Data);

			// Broadcast back to the place we got this advertisement from
			string address = advertisement.Source.AddressOrLocalhost;
			AddAdvertisementAddress(address);

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
			yield return m_UdpClient;
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
			yield return new GenericConsoleCommand<string>("AddAddress", "Adds the address to the list of broadcast destinations", s => AddAdvertisementAddress(s));
			yield return new ConsoleCommand("Broadcast", "Immediately broadcasts the local crosspoints to the network", () => Broadcast());
			yield return new ConsoleCommand("PrintAddresses", "Prints the addresses that advertisements are being sent to", () => PrintAddresses());
		}

		private void PrintAddresses()
		{
			IcdConsole.ConsoleCommandResponseLine("Addresses: {0}", StringUtils.ArrayFormat(GetAdvertisementAddresses()));
			IcdConsole.ConsoleCommandResponse("Ports: {0}", StringUtils.ArrayRangeFormat(GetAdvertisementPorts()));
		}

		#endregion
	}
}
