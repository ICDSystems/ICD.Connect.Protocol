using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using ICD.Connect.Protocol.Network.Ports.Tcp;
using ICD.Connect.Protocol.SerialBuffers;
using Newtonsoft.Json;

namespace ICD.Connect.Protocol.Crosspoints.CrosspointManagers
{
	/// <summary>
	/// The ControlCrosspointManager contains local ControlCrosspoints
	/// and a TCPClient for communication with EquipmentCrosspointManagers.
	/// </summary>
	public sealed class ControlCrosspointManager : AbstractCrosspointManager<IControlCrosspoint>
	{
		private readonly Dictionary<int, ConnectionStateManager> m_ControlClientMap;
		private readonly Dictionary<int, int> m_ControlEquipmentMap;
		private readonly SafeCriticalSection m_ControlMapsSection;

		private readonly TcpClientPool m_ClientPool;
		private readonly TcpClientPoolBufferManager m_BufferManager;

		private bool m_AutoReconnect;

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public override string ConsoleHelp { get { return "Contains the local control crosspoints."; } }

		/// <summary>
		/// When true, attempts to reconnect to equipment on disconnect.
		/// </summary>
		[PublicAPI]
		public bool AutoReconnect
		{
			get
			{
				return m_ControlMapsSection.Execute(() => m_AutoReconnect);
			}
			set
			{
				m_ControlMapsSection.Enter();

				try
				{
					if (value == m_AutoReconnect)
						return;

					m_AutoReconnect = value;

					foreach (ConnectionStateManager manager in m_ControlClientMap.Values)
					{
						if (m_AutoReconnect)
							manager.Start();
						else
							manager.Stop();
					}
				}
				finally
				{
					m_ControlMapsSection.Leave();
				}
			}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public ControlCrosspointManager(int systemId)
			: base(systemId)
		{
			m_ControlClientMap = new Dictionary<int, ConnectionStateManager>();
			m_ControlEquipmentMap = new Dictionary<int, int>();
			m_ControlMapsSection = new SafeCriticalSection();

			m_ClientPool = new TcpClientPool();
			Subscribe(m_ClientPool);

			m_BufferManager = new TcpClientPoolBufferManager(() => new DelimiterSerialBuffer(CrosspointData.MESSAGE_TERMINATOR));
			Subscribe(m_BufferManager);

			m_BufferManager.SetPool(m_ClientPool);

			AutoReconnect = true;
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
			m_ControlMapsSection.Enter();

			try
			{
				IControlCrosspoint unused;
				if (!TryGetCrosspoint(crosspointId, out unused))
				{
					Logger.AddEntry(eSeverity.Warning,
					                "{0} - Failed to connect ControlCrosspoint {1} to EquipmentCrosspoint {2} - No control with given id.",
					                this, crosspointId, equipmentId);
					return eCrosspointStatus.ControlNotFound;
				}

				// Get the host info for the equipment
				CrosspointInfo equipmentInfo;
				if (!RemoteCrosspoints.TryGetCrosspointInfo(equipmentId, out equipmentInfo))
				{
					Logger.AddEntry(eSeverity.Warning,
					                "{0} - Failed to connect ControlCrosspoint {1} to EquipmentCrosspoint {2} - No equipment with given id.",
					                this, crosspointId, equipmentId);
					return eCrosspointStatus.EquipmentNotFound;
				}

				// Get the TCP client from the pool
				AsyncTcpClient client = m_ClientPool.GetClient(equipmentInfo.Host);
				if (!client.IsConnected)
					client.Connect();

				if (!client.IsConnected)
				{
					Logger.AddEntry(eSeverity.Warning,
					                "{0} - Failed to connect ControlCrosspoint {1} to EquipmentCrosspoint {2} - Client failed to connect.",
					                this, crosspointId, equipmentId);
					return eCrosspointStatus.ConnectFailed;
				}

				ConnectionStateManager manager = new ConnectionStateManager(this);
				manager.SetPort(client, AutoReconnect);

				// Add everything to the map
				m_ControlClientMap[crosspointId] = manager;
				m_ControlEquipmentMap[crosspointId] = equipmentId;

				// Send a connect message to the equipment
				CrosspointData message = CrosspointData.ControlConnect(crosspointId, equipmentId);
				client.Send(message.Serialize());
			}
			finally
			{
				m_ControlMapsSection.Leave();
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
			m_ControlMapsSection.Enter();

			try
			{
				ConnectionStateManager manager;
				m_ControlClientMap.TryGetValue(crosspointId, out manager);

				int equipmentId;
				m_ControlEquipmentMap.TryGetValue(crosspointId, out equipmentId);

				if (manager == null)
				{
					Logger.AddEntry(eSeverity.Warning,
					                "{0} - Failed to disconnect ControlCrosspoint {1} - No associated TCP Client.",
					                this, crosspointId);
					return eCrosspointStatus.Idle;
				}

				if (!manager.IsConnected)
				{
					Logger.AddEntry(eSeverity.Warning,
									"{0} - Failed to disconnect ControlCrosspoint {1} - TCP Client is not connected.",
									this, crosspointId);
					return eCrosspointStatus.Idle;
				}

				if (equipmentId == 0)
				{
					Logger.AddEntry(eSeverity.Warning,
					                "{0} - Failed to disconnect ControlCrosspoint {1} - No associated equipment.",
					                this, crosspointId);
					return eCrosspointStatus.Idle;
				}

				// Send the disconnect message
				CrosspointData message = CrosspointData.ControlDisconnect(crosspointId, equipmentId);
				manager.Send(message.Serialize());

				RemoveControlFromDictionaries(crosspointId);
			}
			finally
			{
				m_ControlMapsSection.Leave();
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
			m_ControlMapsSection.Enter();

			try
			{
				return m_ControlEquipmentMap.TryGetValue(controlId, out equipmentId);
			}
			finally
			{
				m_ControlMapsSection.Leave();
			}
		}

		#endregion

		#region Private Methods

		private void RemoveControlFromDictionaries(int crosspointId)
		{
			m_ControlMapsSection.Enter();

			try
			{
				ConnectionStateManager manager;
				m_ControlClientMap.TryGetValue(crosspointId, out manager);

				int equipmentId;
				m_ControlEquipmentMap.TryGetValue(crosspointId, out equipmentId);

				// Remove everything from the dictionaries
				m_ControlClientMap.Remove(crosspointId);
				m_ControlEquipmentMap.Remove(crosspointId);

				// If there are no other controls using this client we can dispose it.
// ReSharper disable AccessToDisposedClosure
				if (manager == null || m_ControlClientMap.Values.Any(m => m == manager))
// ReSharper restore AccessToDisposedClosure
					return;

				m_ClientPool.DisposeClient(manager.Port as AsyncTcpClient);
				manager.Dispose();
			}
			finally
			{
				m_ControlMapsSection.Leave();
			}
		}

		/// <summary>
		/// Gets the TCP client for the given control.
		/// Returns null if the control is not configured with an equipment endpoint.
		/// </summary>
		/// <param name="controlId"></param>
		/// <returns></returns>
		[CanBeNull]
		private ConnectionStateManager LazyLoadClientForControl(int controlId)
		{
			ConnectionStateManager manager;

			m_ControlMapsSection.Enter();

			try
			{
				if (!m_ControlClientMap.TryGetValue(controlId, out manager))
				{
					IControlCrosspoint controlCrosspoint = GetCrosspoint(controlId);
					if (controlCrosspoint.EquipmentCrosspoint == 0)
						return null;

					CrosspointInfo equipmentInfo;
					if (!RemoteCrosspoints.TryGetCrosspointInfo(controlCrosspoint.EquipmentCrosspoint, out equipmentInfo))
						return null;

					AsyncTcpClient client = m_ClientPool.GetClient(equipmentInfo.Host);
					manager = new ConnectionStateManager(this);
					manager.SetPort(client, AutoReconnect);

					m_ControlClientMap.Add(controlId, manager);
				}
			}
			finally
			{
				m_ControlMapsSection.Leave();
			}

			return manager;
		}

		/// <summary>
		/// Instantiates a new crosspoint with the given id and name.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		protected override IControlCrosspoint InstantiateCrosspoint(int id, string name)
		{
			return new ControlCrosspoint(id, name);
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
			m_ControlMapsSection.Enter();

			try
			{
				IEnumerable<ControlCrosspoint> controls =
					m_ControlClientMap.Where(c => c.Value.Port == client)
					                  .Select(c => GetCrosspoint(c.Key))
					                  .OfType<ControlCrosspoint>();

				foreach (ControlCrosspoint control in controls)
					control.Status = connected ? eCrosspointStatus.Connected : eCrosspointStatus.ConnectionDropped;
			}
			finally
			{
				m_ControlMapsSection.Leave();
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
			CrosspointData crosspointData = JsonConvert.DeserializeObject<CrosspointData>(data);

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
						if (crosspoint != null)
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
			ConnectionStateManager manager = LazyLoadClientForControl(crosspoint.Id);
			if (manager == null)
			{
				Logger.AddEntry(eSeverity.Warning, "{0} - Unable to send input data - Control is not connected to an equipment");
				return;
			}

			if (AutoReconnect && !manager.IsConnected)
				manager.Connect();

			if (!manager.IsConnected)
			{
				Logger.AddEntry(eSeverity.Warning, "{0} - Unable to send input data - Unable to connect to remote endpoint");
				return;
			}

			manager.Send(data.Serialize());
		}

		#endregion

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("AutoReconnect", AutoReconnect);
		}

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

			yield return new ConsoleCommand("EnableAutoReconnect", "Turns on automatic reconnection attempts", () => AutoReconnect = true);
			yield return new ConsoleCommand("DisableAutoReconnect", "Turns off automatic reconnection attempts", () => AutoReconnect = false);
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
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			yield return m_ClientPool;
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		/// <summary>
		/// Prints the table of active connections to the console.
		/// </summary>
		private string PrintConnections()
		{
			TableBuilder builder = new TableBuilder("Control", "Equipment", "Equipment Host", "Connected");

			IControlCrosspoint[] controls = GetCrosspoints().ToArray();

			m_ControlMapsSection.Enter();

			try
			{
				foreach (IControlCrosspoint control in controls)
				{
					CrosspointInfo equipmentInfo;
					if (!TryGetEquipmentInfoForControl(control.Id, out equipmentInfo))
						continue;

					ConnectionStateManager manager;
					if (!m_ControlClientMap.TryGetValue(control.Id, out manager))
						continue;

					string controlLabel = string.Format("{0} ({1})", control.Id, control.Name);
					string equipmentLabel = string.Format("{0} ({1})", equipmentInfo.Id, equipmentInfo.Name);

					builder.AddRow(controlLabel, equipmentLabel, equipmentInfo.Host, manager.IsConnected);
				}
			}
			finally
			{
				m_ControlMapsSection.Leave();
			}

			return builder.ToString();
		}

		#endregion
	}
}
