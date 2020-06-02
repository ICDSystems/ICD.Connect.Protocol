using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Network.Ports.Tcp;
using ICD.Connect.Protocol.Network.Servers;
using ICD.Connect.Protocol.SerialBuffers;
using Newtonsoft.Json;

namespace ICD.Connect.Protocol.Crosspoints.CrosspointManagers
{
	/// <summary>
	/// The EquipmentCrosspointManager contains local EquipmentCrosspoints
	/// and a TCPServer for serving equipment to ControlCrosspointManagers.
	/// </summary>
	public sealed class EquipmentCrosspointManager : AbstractCrosspointManager<IEquipmentCrosspoint>
	{
		/// <summary>
		/// Maps each control crosspoint to a TCP client id.
		/// </summary>
		private readonly Dictionary<int, uint> m_ControlClientMap;

		private readonly SafeCriticalSection m_ControlClientMapSection;

		private readonly IcdTcpServer m_Server;
		private readonly NetworkServerBufferManager m_Buffers;

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public override string ConsoleHelp { get { return "Contains the local equipment crosspoints."; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		public EquipmentCrosspointManager(int systemId)
			: base(systemId)
		{
			m_ControlClientMap = new Dictionary<int, uint>();
			m_ControlClientMapSection = new SafeCriticalSection();

			m_Server = new IcdTcpServer
			{
				Port = Xp3Utils.GetPortForSystem(systemId)
			};

			m_Buffers = new NetworkServerBufferManager(() => new DelimiterSerialBuffer(CrosspointData.MESSAGE_TERMINATOR));
			m_Buffers.SetServer(m_Server);

			Subscribe(m_Server);
			Subscribe(m_Buffers);

			m_Server.Start();
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public override void Dispose()
		{
			base.Dispose();

			Unsubscribe(m_Server);
			Unsubscribe(m_Buffers);

			m_Server.Stop();

			m_Buffers.Dispose();
			m_Server.Dispose();
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Returns the controls for the given client.
		/// </summary>
		/// <param name="clientId"></param>
		/// <returns></returns>
		private IEnumerable<int> GetControlIds(uint clientId)
		{
			m_ControlClientMapSection.Enter();

			try
			{
				return m_ControlClientMap.Where(kvp => kvp.Value == clientId)
				                         .Select(kvp => kvp.Key)
				                         .Order()
				                         .ToArray();
			}
			finally
			{
				m_ControlClientMapSection.Leave();
			}
		}

		/// <summary>
		/// Adds the control to the equipment and the client map.
		/// </summary>
		/// <param name="controlId"></param>
		/// <param name="equipmentId"></param>
		/// <param name="clientId"></param>
		private void AddControlId(int controlId, int equipmentId, uint clientId)
		{
			m_ControlClientMapSection.Enter();

			try
			{
				// Add the control to the client map
				m_ControlClientMap[controlId] = clientId;

				// Add the control to the equipment crosspoint
				GetCrosspoint(equipmentId).Initialize(controlId);
			}
			finally
			{
				m_ControlClientMapSection.Leave();
			}
		}

		/// <summary>
		/// Removes the control from the internal equipment->control map.
		/// </summary>
		/// <param name="controlId"></param>
		private void RemoveControlId(int controlId)
		{
			m_ControlClientMapSection.Enter();

			try
			{
				// Remove the control from the equipment crosspoint
				foreach (IEquipmentCrosspoint crosspoint in GetCrosspoints())
					crosspoint.Deinitialize(controlId);

				// Remove the control from the client map
				m_ControlClientMap.Remove(controlId);
			}
			finally
			{
				m_ControlClientMapSection.Leave();
			}
		}

		/// <summary>
		/// Removes the client from the internal equipment->control map.
		/// </summary>
		/// <param name="client"></param>
		private void RemoveClient(uint client)
		{
			m_ControlClientMapSection.Enter();

			try
			{
				foreach (int controlId in GetControlIds(client))
					RemoveControlId(controlId);
			}
			finally
			{
				m_ControlClientMapSection.Leave();
			}
		}

		/// <summary>
		/// Instantiates a new crosspoint with the given id and name.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		protected override IEquipmentCrosspoint InstantiateCrosspoint(int id, string name)
		{
			return new EquipmentCrosspoint(id, name);
		}

		#endregion

		#region Server Callbacks

		/// <summary>
		/// Subscribe to the server events.
		/// </summary>
		/// <param name="server"></param>
		private void Subscribe(IcdTcpServer server)
		{
			server.OnSocketStateChange += ServerOnSocketStateChange;
		}

		/// <summary>
		/// Unsubscribe from the server events.
		/// </summary>
		/// <param name="server"></param>
		private void Unsubscribe(IcdTcpServer server)
		{
			server.OnSocketStateChange += ServerOnSocketStateChange;
		}

		/// <summary>
		/// Called when a client socket state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void ServerOnSocketStateChange(object sender, SocketStateEventArgs args)
		{
			uint client = args.ClientId;
			if (m_Server.ClientConnected(client))
				return;

			// When a client disconnects we remove it from the map
			RemoveClient(client);
		}

		#endregion

		#region Buffer Manager Callbacks

		/// <summary>
		/// Subscribe to the buffer events.
		/// </summary>
		/// <param name="bufferManager"></param>
		private void Subscribe(NetworkServerBufferManager bufferManager)
		{
			bufferManager.OnClientCompletedSerial += BufferManagerOnClientCompletedSerial;
		}

		/// <summary>
		/// Unsubscribe from the buffer events.
		/// </summary>
		/// <param name="bufferManager"></param>
		private void Unsubscribe(NetworkServerBufferManager bufferManager)
		{
			bufferManager.OnClientCompletedSerial -= BufferManagerOnClientCompletedSerial;
		}

		/// <summary>
		/// Called when a complete string is received from a control crosspoint.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="clientId"></param>
		/// <param name="data"></param>
		private void BufferManagerOnClientCompletedSerial(NetworkServerBufferManager sender, uint clientId, string data)
		{
			CrosspointData crosspointData = JsonConvert.DeserializeObject<CrosspointData>(data);

			switch (crosspointData.MessageType)
			{
				case CrosspointData.eMessageType.ControlConnect:
					// Register the ControlIds
					foreach (int controlId in crosspointData.GetControlIds())
						AddControlId(controlId, crosspointData.EquipmentId, clientId);
					break;

				case CrosspointData.eMessageType.ControlDisconnect:
					// Unregister the ControlIds
					foreach (int controlId in crosspointData.GetControlIds())
						RemoveControlId(controlId);
					break;
			}

			// Send the data to the equipment
			IEquipmentCrosspoint equipment;
			if (TryGetCrosspoint(crosspointData.EquipmentId, out equipment))
				SendCrosspointOutputData(equipment, crosspointData);
		}

		#endregion

		#region Crosspoint Callbacks

		/// <summary>
		/// Called when the crosspoint outputs data to be sent over the network.
		/// </summary>
		/// <param name="crosspoint"></param>
		/// <param name="data"></param>
		protected override void CrosspointOnSendInputData(IEquipmentCrosspoint crosspoint, CrosspointData data)
		{
			if (m_Server.NumberOfClients == 0)
				return;

			if (crosspoint.ControlCrosspointsCount == 0)
				return;

			m_Server.Send(data.Serialize());
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			yield return m_Server;
		}

		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		#endregion
	}
}
