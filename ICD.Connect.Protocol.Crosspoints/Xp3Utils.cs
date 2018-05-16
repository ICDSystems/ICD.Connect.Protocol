using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Connect.Protocol.Network.Ports.Tcp;

namespace ICD.Connect.Protocol.Crosspoints
{
	public static class Xp3Utils
	{
		/// <summary>
		/// Max number of clients accepted by the EquipmentCrosspointManager TCP Server.
		/// </summary>
		public const int MAX_NUMBER_OF_CLIENTS = AsyncTcpServer.MAX_NUMBER_OF_CLIENTS_SUPPORTED;

		/// <summary>
		/// A Equipment id of 0 is invalid, so represents no equipment connected.
		/// </summary>
		public const int NULL_EQUIPMENT = 0;

		private const ushort BASE_EQUIPMENT_MANAGER_PORT = 30000;

		/// <summary>
		/// Crestron does not support multiple programs on the same processor using UDP Servers
		/// with the same port. As a result, we loop from 1 to PROGRAM_SLOT_COUNT to advertise to
		/// all programs.
		/// 
		/// This is assuming that there will only ever be 10 slots on a processor. Maybe in 10
		/// years Crestron will release a CP4 with 100 slots and this code will break.
		/// </summary>
		private const uint PROGRAM_SLOT_COUNT = 10;

		public const string MULTICAST_ADDRESS = "239.64.0.1";
		public const string LOCALHOST_ADDRESS = "127.0.0.1";

		#region Methods

		/// <summary>
		/// Enumerates over the UDP advertisement ports for the different program slots. 
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<ushort> GetAdvertisementMulticastPorts(int systemId)
		{
			for (uint index = 0; index < PROGRAM_SLOT_COUNT; index++)
				yield return GetPortForSlotAndSystem(index + 1, systemId);
		}

		/// <summary>
		/// Generates a port that is unique for the given program slot and system id.
		/// </summary>
		/// <param name="programNumber"></param>
		/// <param name="systemId"></param>
		/// <returns></returns>
		public static ushort GetPortForSlotAndSystem(uint programNumber, int systemId)
		{
			uint slotNumber = programNumber - 1;
			return (ushort)(BASE_EQUIPMENT_MANAGER_PORT + systemId * 10 + slotNumber);
		}

		/// <summary>
		/// Generates a port for the current program slot and given system id.
		/// </summary>
		/// <param name="systemId"></param>
		/// <returns></returns>
		public static ushort GetPortForSystem(int systemId)
		{
			return GetPortForSlotAndSystem(ProgramUtils.ProgramNumber, systemId);
		}

		#endregion
	}
}
