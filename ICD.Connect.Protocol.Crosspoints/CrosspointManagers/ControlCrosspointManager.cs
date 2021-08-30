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
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using ICD.Connect.Protocol.Network.Ports.Tcp;
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

		private readonly BiDictionary<IcdTcpClient, ConnectionStateManager> m_ClientToCsm;
		private readonly Dictionary<int, IcdTcpClient> m_ControlClientMap;
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

					foreach (ConnectionStateManager manager in m_ClientToCsm.Values)
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
			m_ClientToCsm = new BiDictionary<IcdTcpClient, ConnectionStateManager>();
			m_ControlClientMap = new Dictionary<int, IcdTcpClient>();
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
		public eCrosspointStatus ConnectCrosspoint([NotNull] IControlCrosspoint crosspoint, int equipmentId)
		{
			if (crosspoint == null)
				throw new ArgumentNullException("crosspoint");

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
				IcdTcpClient client = m_ClientPool.GetClient(equipmentInfo.Host);
				IcdConsole.PrintLine(eConsoleColor.Magenta, "Lazy loaded TCP client for host {0} - {1}", equipmentInfo.Host, client);
				if (!client.IsConnected)
					client.Connect();

				if (!client.IsConnected)
				{
					Logger.AddEntry(eSeverity.Warning,
					                "{0} - Failed to connect ControlCrosspoint {1} to EquipmentCrosspoint {2} - Client failed to connect.",
					                this, crosspointId, equipmentId);
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
		public eCrosspointStatus DisconnectCrosspoint([NotNull] IControlCrosspoint crosspoint)
		{
			if (crosspoint == null)
				throw new ArgumentNullException("crosspoint");

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
				IcdTcpClient client;
				m_ControlClientMap.TryGetValue(crosspointId, out client);

				int equipmentId;
				m_ControlEquipmentMap.TryGetValue(crosspointId, out equipmentId);

				if (client == null)
				{
					Logger.AddEntry(eSeverity.Warning,
					                "{0} - Failed to send disconnect message for ControlCrosspoint {1} - No associated TCP Client.",
					                this, crosspointId);
				}
				else if (!client.IsConnected)
				{
					Logger.AddEntry(eSeverity.Warning,
									"{0} - Failed to send disconnect message for  ControlCrosspoint {1} - TCP Client is not connected.",
									this, crosspointId);
				}
				else if (equipmentId == 0)
				{
					Logger.AddEntry(eSeverity.Warning,
					                "{0} - Failed to send disconnect message for  ControlCrosspoint {1} - No associated equipment.",
					                this, crosspointId);
				}
				else
				{
					// Send the disconnect message
					CrosspointData message = CrosspointData.ControlDisconnect(crosspointId, equipmentId);
					client.Send(message.Serialize());
				}

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
				IcdTcpClient client;
				m_ControlClientMap.TryGetValue(crosspointId, out client);

				int equipmentId;
				m_ControlEquipmentMap.TryGetValue(crosspointId, out equipmentId);

				// Remove everything from the dictionaries
				m_ControlClientMap.Remove(crosspointId);
				m_ControlEquipmentMap.Remove(crosspointId);

				// If there are no other controls using this client we can dispose it.
// ReSharper disable AccessToDisposedClosure
				if (client == null || m_ControlClientMap.Values.Any(m => m == client))
// ReSharper restore AccessToDisposedClosure
					return;

				m_ClientPool.DisposeClient(client, CLIENT_KEEP_ALIVE);
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
		private IcdTcpClient LazyLoadClientForControl(int controlId)
		{
			IcdTcpClient client;

			m_ControlMapsSection.Enter();

			try
			{
				if (!m_ControlClientMap.TryGetValue(controlId, out client))
				{
					IControlCrosspoint controlCrosspoint = GetCrosspoint(controlId);
					if (controlCrosspoint.EquipmentCrosspoint == 0)
						return null;

					CrosspointInfo equipmentInfo;
					if (!RemoteCrosspoints.TryGetCrosspointInfo(controlCrosspoint.EquipmentCrosspoint, out equipmentInfo))
					{
						Logger.AddEntry(eSeverity.Error, "XP3 ControlCrosspoing Manager - Unable to find remote equipment crosspoint {1} for local control crosspoint {0}", controlCrosspoint.Id, controlCrosspoint.EquipmentCrosspoint );
						return null;
					}
					client = m_ClientPool.GetClient(equipmentInfo.Host);
					IcdConsole.PrintLine(eConsoleColor.Magenta, "Lazy loaded TCP client for host {0} - {1}", equipmentInfo.Host, client);

					m_ControlClientMap.Add(controlId, client);
				}
			}
			finally
			{
				m_ControlMapsSection.Leave();
			}

			return client;
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

		/// <summary>
		/// Sends data from the program to the crosspoint.
		/// </summary>
		/// <param name="crosspoint"></param>
		/// <param name="data"></param>
		protected override void SendCrosspointOutputData([NotNull] IControlCrosspoint crosspoint,
		                                                 [NotNull] CrosspointData data)
		{
			if (crosspoint == null)
				throw new ArgumentNullException("crosspoint");
			if (data == null)
				throw new ArgumentNullException("data");

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

		#region TCP Client Pool callbacks

		/// <summary>
		/// Subscribe to the client pool events.
		/// </summary>
		/// <param name="pool"></param>
		private void Subscribe([NotNull] TcpClientPool pool)
		{
			if (pool == null)
				throw new ArgumentNullException("pool");

			pool.OnClientConnectionStateChanged += PoolOnClientConnectionStateChanged;
			pool.OnClientAdded += PoolOnClientAdded;
			pool.OnClientRemoved += PoolOnClientRemoved;
		}

		/// <summary>
		/// Called when a client connection state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="client"></param>
		/// <param name="connected"></param>
		private void PoolOnClientConnectionStateChanged(TcpClientPool sender, IcdTcpClient client, bool connected)
		{
			m_ControlMapsSection.Enter();

			try
			{
				IEnumerable<ControlCrosspoint> controls =
					m_ControlClientMap.Where(kvp => kvp.Value == client)
					                  .Select(c => GetCrosspoint(c.Key))
					                  .OfType<ControlCrosspoint>();

				// Send a fresh connection message
				foreach (ControlCrosspoint control in controls)
				{
					control.Status = connected ? eCrosspointStatus.Connected : eCrosspointStatus.ConnectionDropped;

					if (connected)
					{
						CrosspointData message = CrosspointData.ControlConnect(control.Id, control.EquipmentCrosspoint);
						client.Send(message.Serialize());
					}
				}
			}
			finally
			{
				m_ControlMapsSection.Leave();
			}
		}

		/// <summary>
		/// When a new client is spun up we create a ConnectionStateManager for it.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="client"></param>
		private void PoolOnClientAdded(TcpClientPool sender, IcdTcpClient client)
		{
			m_ControlMapsSection.Enter();

			try
			{
				ConnectionStateManager csm = new ConnectionStateManager(this);
				m_ControlMapsSection.Execute(() => m_ClientToCsm.Add(client, csm));

				csm.SetPort(client, AutoReconnect);
			}
			finally
			{
				m_ControlMapsSection.Leave();
			}
		}

		/// <summary>
		/// When a client is removed we destroy the associated ConnectionStateManager as
		/// well as the client itself.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="client"></param>
		private void PoolOnClientRemoved(TcpClientPool sender, IcdTcpClient client)
		{
			m_ControlMapsSection.Enter();

			try
			{
				ConnectionStateManager csm;
				if (m_ClientToCsm.TryGetValue(client, out csm))
					csm.Dispose();

				m_ClientToCsm.RemoveKey(client);

				client.Dispose();
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
		private void Subscribe([NotNull] TcpClientPoolBufferManager bufferManager)
		{
			if (bufferManager == null)
				throw new ArgumentNullException("bufferManager");

			bufferManager.OnClientCompletedSerial += BufferManagerOnClientCompletedSerial;
		}

		/// <summary>
		/// Unsubscribe from the buffer manager events.
		/// </summary>
		/// <param name="bufferManager"></param>
		private void Unsubscribe([NotNull] TcpClientPoolBufferManager bufferManager)
		{
			if (bufferManager == null)
				throw new ArgumentNullException("bufferManager");

			bufferManager.OnClientCompletedSerial -= BufferManagerOnClientCompletedSerial;
		}

		/// <summary>
		/// Called when we receive a complete JSON object from an equipment crosspoint manager.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="client"></param>
		/// <param name="data"></param>
		private void BufferManagerOnClientCompletedSerial(TcpClientPoolBufferManager sender, IcdTcpClient client,
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

		#endregion

		#region Crosspoint Callbacks

		/// <summary>
		/// Subscribe to the crosspoint events.
		/// </summary>
		/// <param name="crosspoint"></param>
		protected override void Subscribe([NotNull] IControlCrosspoint crosspoint)
		{
			if (crosspoint == null)
				throw new ArgumentNullException("crosspoint");

			base.Subscribe(crosspoint);

			crosspoint.RequestConnectCallback = ConnectCrosspoint;
			crosspoint.RequestDisconnectCallback = DisconnectCrosspoint;
		}

		/// <summary>
		/// Unsubscribe from the crosspoint events.
		/// </summary>
		/// <param name="crosspoint"></param>
		protected override void Unsubscribe([NotNull] IControlCrosspoint crosspoint)
		{
			if (crosspoint == null)
				throw new ArgumentNullException("crosspoint");

			base.Unsubscribe(crosspoint);

			crosspoint.RequestConnectCallback = null;
			crosspoint.RequestDisconnectCallback = null;
		}

		/// <summary>
		/// Called when the control crosspoint raises data to be sent over the network.
		/// </summary>
		/// <param name="crosspoint"></param>
		/// <param name="data"></param>
		protected override void CrosspointOnSendInputData([NotNull] IControlCrosspoint crosspoint,
		                                                  [NotNull] CrosspointData data)
		{
			if (crosspoint == null)
				throw new ArgumentNullException("crosspoint");
			if (data == null)
				throw new ArgumentNullException("data");

			IcdTcpClient client = LazyLoadClientForControl(crosspoint.Id);
			if (client == null)
			{
				Logger.AddEntry(eSeverity.Warning, "{0} - Unable to send input data - Control {0} is not able to connect to equipment {1}", crosspoint.Id, crosspoint.EquipmentCrosspoint);
				return;
			}

			if (AutoReconnect && !client.IsConnected)
				client.Connect();

			if (!client.IsConnected)
			{
				Logger.AddEntry(eSeverity.Warning, "{0} - Unable to send input data - Control {0} unable to connect to remote endpoint equipment {1}", crosspoint.Id, crosspoint.EquipmentCrosspoint);
				return;
			}

			client.Send(data.Serialize());
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

					IcdTcpClient client;
					if (!m_ControlClientMap.TryGetValue(control.Id, out client))
						continue;

					string controlLabel = string.Format("{0} ({1})", control.Id, control.Name);
					string equipmentLabel = string.Format("{0} ({1})", equipmentInfo.Id, equipmentInfo.Name);

					builder.AddRow(controlLabel, equipmentLabel, equipmentInfo.Host, client.IsConnected);
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
