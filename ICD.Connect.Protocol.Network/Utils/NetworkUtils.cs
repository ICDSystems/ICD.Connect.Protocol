using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;

namespace ICD.Connect.Protocol.Network.Utils
{
	public static class NetworkUtils
	{
		private const ushort BASE_BROADCAST_PORT = 31000;
		private const ushort BASE_DIRECT_PORT = 31000;

		/// <summary>
		/// Crestron does not support multiple programs on the same processor using UDP Servers
		/// with the same port. As a result, we loop from 1 to PROGRAM_SLOT_COUNT to advertise to
		/// all programs.
		/// 
		/// This is assuming that there will only ever be 10 slots on a processor. Maybe in 10
		/// years Crestron will release a CP4 with 100 slots and this code will break.
		/// </summary>
		private const int PROGRAM_SLOT_COUNT = 10;

		public const string MULTICAST_ADDRESS = "239.64.0.1";
		public const string LOCALHOST_ADDRESS = "127.0.0.1";

		#region Methods

		/// <summary>
		/// Enumerates over the UDP advertisement ports for the different program slots. 
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<ushort> GetBroadcastPorts(int systemId)
		{
			return Enumerable.Range(1, PROGRAM_SLOT_COUNT).Select(index => GetBroadcastPortForProgramSlot((uint)index, systemId));
		}

		/// <summary>
		/// Generates a port that is unique for the given program slot and system id.
		/// </summary>
		/// <param name="programNumber"></param>
		/// <param name="systemId"></param>
		/// <returns></returns>
		public static ushort GetBroadcastPortForProgramSlot(uint programNumber, int systemId)
		{
			uint slotNumber = programNumber - 1;
			return (ushort)(BASE_BROADCAST_PORT + systemId * 10 + slotNumber);
		}

		/// <summary>
		/// Generates a port for the current program slot and given system id.
		/// </summary>
		/// <param name="systemId"></param>
		/// <returns></returns>
		public static ushort GetBroadcastPortForSystem(int systemId)
		{
			return GetBroadcastPortForProgramSlot(ProgramUtils.ProgramNumber, systemId);
		}

		#endregion

		/// <summary>
		/// Enumerates over the UDP advertisement ports for the different program slots. 
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<ushort> GetDirectMessagePorts(int systemId)
		{
			return Enumerable.Range(1, PROGRAM_SLOT_COUNT).Select(index => GetDirectMessagePortForProgramSlot((uint)index, systemId));
		}

		/// <summary>
		/// Generates a port that is unique for the given program slot and system id.
		/// </summary>
		/// <param name="programNumber"></param>
		/// <param name="systemId"></param>
		/// <returns></returns>
		public static ushort GetDirectMessagePortForProgramSlot(uint programNumber, int systemId)
		{
			uint slotNumber = programNumber - 1;
			return (ushort)(BASE_DIRECT_PORT + systemId * 10 + slotNumber);
		}

		/// <summary>
		/// Generates a port for the current program slot and given system id.
		/// </summary>
		/// <param name="systemId"></param>
		/// <returns></returns>
		public static ushort GetDirectMessagePortForSystem(int systemId)
		{
			return GetDirectMessagePortForProgramSlot(ProgramUtils.ProgramNumber, systemId);
		}
	}
}
