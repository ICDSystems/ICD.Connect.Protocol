using System.Collections.Generic;
using System.Linq;
using ICD.Connect.API.Commands;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Connect.Protocol.Network.Tcp;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using ICD.Connect.Protocol.SerialBuffers;

namespace ICD.Connect.Protocol.Crosspoints.CrosspointManagers
{
	/// <summary>
	/// The ControlCrosspointManager contains local ControlCrosspoints
	/// and a TCPClient for communication with EquipmentCrosspointManagers.
	/// </summary>
	public sealed class ControlCrosspointManager : AbstractCrosspointManager<IControlCrosspoint>
	{
		/// <summary>
		/// How long, in milliseconds, to keep a TCP client alive after all controls are done with it.
		/// </summary>
		private const long CLIENT_KEEP_ALIVE = 60 * 1000;

		private readonly Dictionary<int, AsyncTcpClient> m_ControlClientMap;
		private readonly Dictionary<int, int> m_ControlEquipmentMap;
		private readonly SafeCriticalSection m_ControlClientMapSection;

		private readonly TcpClientPool m_ClientPool;
		private readonly TcpClientPoolBufferManager m_BufferManager;

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public override string ConsoleHelp { get { return "Contains the local control crosspoints."; } }

		/// <summary>
		/// When true, attempts to reconnect to equipment on disconnect.
		/// </summary>
		[PublicAPI]
		public bool AutoReconnect { get; set; }

		/// <summary>
		/// Constructor.
		/// </summary>
		public ControlCrosspointManager(int systemId)
			: base(systemId)
		{
			m_ControlClientMap = new Dictionary<int, AsyncTcpClient>();
			m_ControlEquipmentMap = new Dictionary<int, int>();
			m_ControlClientMapSection = new SafeCriticalSection();

			m_ClientPool = new TcpClientPool();
			Subscribe(m_ClientPool);

			m_BufferManager = new TcpClientPoolBufferManager(() => new DelimiterSerialBuffer(CrosspointData.MESSAGE_TERMINATOR));
			Subscribe(m_BufferManager);

			m_BufferManager.SetPool(m_ClientPool);
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public override void Dispose()
		{
			base.Dispose();

			Unsubscribe(m_BufferManager);

			m_BufferManager.Dispose();
			m_ClientPool.Dispose();
		}

		/// <summary>
		/// Connects the crosspoint to the equipment with the given id.
		/// </summary>
		/// <param name="crosspoint"></param>
		/// <param name="equipmentId"></param>
		/// <returns>False if connection failed.</returns>
		[PublicAPI]
		public eCrosspointStatus ConnectCrosspoint(IControlCrosspoint crosspoint, int equipmentId)
		{
			return ConnectCrosspoint(crosspoint.Id, equipmentId);
		}

		/// <summary>
		/// Connects the crosspoint to the equipment with the given id.
		/// </summary>
		/// <param name="crosspointId"></param>
		/// <param name="equipmentId"></param>
		/// <returns>False if connection failed.</returns>
		[PublicAPI]
		public eCrosspointStatus ConnectCrosspoint(int crosspointId, int equipmentId)
		{			
			m_ControlClientMapSection.Enter();

			try
			{
				// Get the host info for the equipment
				CrosspointInfo equipmentInfo;
				if (!RemoteCrosspoints.TryGetCrosspointInfo(equipmentId, out equipmentInfo))
				{
					IcdErrorLog.Warn("Failed to connect ControlCrosspoint {0} to EquipmentCrosspoint {1} - No equipment with given id.",
									 crosspointId, equipmentId);
					return eCrosspointStatus.EquipmentNotFound;
				}

				// Get the TCP client from the pool
				AsyncTcpClient client = m_ClientPool.GetClient(equipmentInfo.Host);
				if (!client.IsConnected)
					client.Connect();

				if (!client.IsConnected)
				{
					IcdErrorLog.Warn("Failed to connect ControlCrosspoint {0} to EquipmentCrosspoint {1} - Client failed to connect.",
									 crosspointId, equipmentId);
					return eCrosspointStatus.ConnectFailed;
				}

				// Add everything to the map
				m_ControlClientMap[crosspointId] = client;
				m_ControlEquipmentMap[crosspointId] = equipmentId;

				// Send a connect message to the equipment
				CrosspointData message = CrosspointData.ControlConnect(crosspointId, equipmentId);
				client.Send(message.Serialize());
			}
			finally
			{
				m_ControlClientMapSection.Leave();
			}

			return eCrosspointStatus.Connected;
		}

		/// <summary>
		/// Disconnects the crosspoint from its current equipment manager.
		/// </summary>
		/// <param name="crosspoint"></param>
		/// <returns>False if disconnect failed.</returns>
		[PublicAPI]
		public eCrosspointStatus DisconnectCrosspoint(IControlCrosspoint crosspoint)
		{
			return DisconnectCrosspoint(crosspoint.Id);
		}

		/// <summary>
		/// Disconnects the crosspoint from its current equipment manager.
		/// </summary>
		/// <param name="crosspointId"></param>
		/// <returns>False if disconnect failed.</returns>
		[PublicAPI]
		public eCrosspointStatus DisconnectCrosspoint(int crosspointId)
		{
			m_ControlClientMapSection.Enter();

			try
			{
				AsyncTcpClient client;
				m_ControlClientMap.TryGetValue(crosspointId, out client);

				int equipmentId;
				m_ControlEquipmentMap.TryGetValue(crosspointId, out equipmentId);

				if (client == null)
				{
					IcdErrorLog.Warn("Failed to disconnect ControlCrosspoint {0} - No associated TCP Client.", crosspointId);
					return eCrosspointStatus.Idle;
				}

				if (equipmentId == 0)
				{
					IcdErrorLog.Warn("Failed to disconnect ControlCrosspoint {0} - No associated equipment.", crosspointId);
					return eCrosspointStatus.Idle;
				}

				// Send the disconnect message
				CrosspointData message = CrosspointData.ControlDisconnect(crosspointId, equipmentId);
				client.Send(message.Serialize());

				RemoveControlFromDictionaries(crosspointId);
			}
			finally
			{
				m_ControlClientMapSection.Leave();
			}

			return eCrosspointStatus.Idle;
		}

		/// <summary>
		/// Gets info for the equipment that the control is currently connected to.
		/// </summary>
		/// <param name="controlId"></param>
		/// <param name="equipmentInfo"></param>
		/// <returns></returns>
		[PublicAPI]
		public bool TryGetEquipmentInfoForControl(int controlId, out CrosspointInfo equipmentInfo)
		{
			equipmentInfo = new CrosspointInfo();
			int equipmentId;

			return TryGetEquipmentForControl(controlId, out equipmentId) &&
			       RemoteCrosspoints.TryGetCrosspointInfo(equipmentId, out equipmentInfo);
		}

		/// <summary>
		/// Gets the equipment id for the given control id.
		/// </summary>
		/// <param name="controlId"></param>
		/// <param name="equipmentId"></param>
		/// <returns></returns>
		[PublicAPI]
		public bool TryGetEquipmentForControl(int controlId, out int equipmentId)
		{
			m_ControlClientMapSection.Enter();

			try
			{
				return m_ControlEquipmentMap.TryGetValue(controlId, out equipmentId);
			}
			finally
			{
				m_ControlClientMapSection.Leave();
			}
		}

		#endregion

		#region Private Methods

		private void RemoveControlFromDictionaries(int crosspointId)
		{
			m_ControlClientMapSection.Enter();

			try
			{
				AsyncTcpClient client;
				m_ControlClientMap.TryGetValue(crosspointId, out client);

				int equipmentId;
				m_ControlEquipmentMap.TryGetValue(crosspointId, out equipmentId);

				// Remove everything from the dictionaries
				m_ControlClientMap.Remove(crosspointId);
				m_ControlEquipmentMap.Remove(crosspointId);

				// If there are no other controls using this client we can remove it.
				if (m_ControlClientMap.Values.All(c => c != client))
					m_ClientPool.DisposeClient(client, CLIENT_KEEP_ALIVE);
			}
			finally
			{
				m_ControlClientMapSection.Leave();
			}
		}

		/// <summary>
		/// Gets the TCP client for the given control.
		/// Returns null if no client found.
		/// </summary>
		/// <param name="controlId"></param>
		/// <returns></returns>
		[CanBeNull]
		private AsyncTcpClient GetClientForControl(int controlId)
		{
			AsyncTcpClient client;

			m_ControlClientMapSection.Enter();

			try
			{
				if (!m_ControlClientMap.TryGetValue(controlId, out client))
					return null;
			}
			finally
			{
				m_ControlClientMapSection.Leave();
			}

			return client;
		}

		#endregion

		#region TCP Client Pool callbacks

		/// <summary>
		/// Subscribe to the client pool events.
		/// </summary>
		/// <param name="pool"></param>
		private void Subscribe(TcpClientPool pool)
		{
			pool.OnClientConnectionStateChanged += PoolOnClientConnectionStateChanged;
		}

		/// <summary>
		/// Called when a client connection state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="client"></param>
		/// <param name="connected"></param>
		private void PoolOnClientConnectionStateChanged(TcpClientPool sender, AsyncTcpClient client, bool connected)
		{
			// If the client disconnects attempt to reconnect it
			if (!connected && AutoReconnect)
				client.Connect();

			if (client.IsConnected)
				return;

			m_ControlClientMapSection.Enter();

			try
			{
				IEnumerable<ControlCrosspoint> controls =
					m_ControlClientMap.Where(c => c.Value == client)
					                  .Select(c => GetCrosspoint(c.Key))
					                  .OfType<ControlCrosspoint>();

				foreach (ControlCrosspoint control in controls)
					control.Status = eCrosspointStatus.ConnectionDropped;
			}
			finally
			{
				m_ControlClientMapSection.Leave();
			}
		}

		#endregion

		#region Buffer Manager callbacks

		/// <summary>
		/// Subscribe to the buffer manager events.
		/// </summary>
		/// <param name="bufferManager"></param>
		private void Subscribe(TcpClientPoolBufferManager bufferManager)
		{
			bufferManager.OnClientCompletedSerial += BufferManagerOnClientCompletedSerial;
		}

		/// <summary>
		/// Unsubscribe from the buffer manager events.
		/// </summary>
		/// <param name="bufferManager"></param>
		private void Unsubscribe(TcpClientPoolBufferManager bufferManager)
		{
			bufferManager.OnClientCompletedSerial -= BufferManagerOnClientCompletedSerial;
		}

		/// <summary>
		/// Called when we receive a complete JSON object from an equipment crosspoint manager.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="client"></param>
		/// <param name="data"></param>
		private void BufferManagerOnClientCompletedSerial(TcpClientPoolBufferManager sender, AsyncTcpClient client,
														  string data)
		{
			CrosspointData crosspointData = CrosspointData.Deserialize(data);

			switch (crosspointData.MessageType)
			{
				case CrosspointData.eMessageType.EquipmentDisconnect:
					// Unregister the ControlIds
					foreach (int controlId in crosspointData.GetControlIds())
					{
						int equipment;
						if (!TryGetEquipmentForControl(controlId, out equipment) || equipment != crosspointData.EquipmentId)
							continue;

						RemoveControlFromDictionaries(controlId);

						ControlCrosspoint crosspoint = GetCrosspoint(controlId) as ControlCrosspoint;
						if(crosspoint != null)
							crosspoint.Status = eCrosspointStatus.ConnectionClosedRemote;
					}
					break;
			}

			foreach (int controlId in crosspointData.GetControlIds())
			{
				// Do we have the crosspoint with the given id?
				IControlCrosspoint crosspoint;
				if (TryGetCrosspoint(controlId, out crosspoint))
					SendCrosspointOutputData(crosspoint, crosspointData);
			}
		}

		/// <summary>
		/// Sends data from the program to the crosspoint.
		/// </summary>
		/// <param name="crosspoint"></param>
		/// <param name="data"></param>
		protected override void SendCrosspointOutputData(IControlCrosspoint crosspoint, CrosspointData data)
		{
			// Is the control connected to equipment?
			int equipmentId;
			if (!TryGetEquipmentForControl(crosspoint.Id, out equipmentId))
				return;

			// Is the control connected to the equipment that sent this data?
			if (equipmentId != data.EquipmentId)
				return;

			base.SendCrosspointOutputData(crosspoint, data);
		}

		#endregion

		#region Crosspoint Callbacks

		/// <summary>
		/// Subscribe to the crosspoint events.
		/// </summary>
		/// <param name="crosspoint"></param>
		protected override void Subscribe(IControlCrosspoint crosspoint)
		{
			base.Subscribe(crosspoint);

			crosspoint.RequestConnectCallback = ConnectCrosspoint;
			crosspoint.RequestDisconnectCallback = DisconnectCrosspoint;
		}

		/// <summary>
		/// Unsubscribe from the crosspoint events.
		/// </summary>
		/// <param name="crosspoint"></param>
		protected override void Unsubscribe(IControlCrosspoint crosspoint)
		{
			base.Unsubscribe(crosspoint);

			crosspoint.RequestConnectCallback = null;
			crosspoint.RequestDisconnectCallback = null;
		}

		/// <summary>
		/// Called when the control crosspoint raises data to be sent over the network.
		/// </summary>
		/// <param name="crosspoint"></param>
		/// <param name="data"></param>
		protected override void CrosspointOnSendInputData(IControlCrosspoint crosspoint, CrosspointData data)
		{
			// If the crosspoint isn't currently connected just drop the data
			AsyncTcpClient client = GetClientForControl(crosspoint.Id);
			if (client == null || !client.IsConnected)
				return;

			client.Send(data.Serialize());
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return
				new ConsoleCommand("PrintConnections", "Prints the controls that are currently connected to equipment",
				                   () => PrintConnections());
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		/// <summary>
		/// Prints the table of active connections to the console.
		/// </summary>
		private string PrintConnections()
		{
			TableBuilder builder = new TableBuilder("Control", "Equipment", "Equipment Host");

			IControlCrosspoint[] controls = GetCrosspoints().ToArray();

			m_ControlClientMapSection.Enter();

			try
			{
				foreach (IControlCrosspoint control in controls)
				{
					CrosspointInfo equipmentInfo;
					if (!TryGetEquipmentInfoForControl(control.Id, out equipmentInfo))
						continue;

					string controlLabel = string.Format("{0} ({1})", control.Id, control.Name);
					string equipmentLabel = string.Format("{0} ({1})", equipmentInfo.Id, equipmentInfo.Name);

					builder.AddRow(controlLabel, equipmentLabel, equipmentInfo.Host);
				}
			}
			finally
			{
				m_ControlClientMapSection.Leave();
			}

			return builder.ToString();
		}

		#endregion
	}
}
