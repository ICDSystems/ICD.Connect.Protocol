﻿using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Sigs;

namespace ICD.Connect.Protocol.Crosspoints.Crosspoints
{
	/// <summary>
	/// ControlCrosspoints typically represent a user interface device like
	/// a touch panel or wall plate.
	/// </summary>
	public sealed class ControlCrosspoint : AbstractCrosspoint, IControlCrosspoint
	{
		// We cache changed sigs for the clear operation.
		private readonly SigCache m_SigCache;
		private readonly SafeCriticalSection m_SigCacheSection;
		private readonly SafeCriticalSection m_InitializeSection;
		private bool m_SigsWaitingToBeCleared;
		private int m_EquipmentCrosspoint;

		#region Properties

		/// <summary>
		/// Raised when Initialize is called. Typically used by the parent
		/// crosspoint manager to establish a network connection to the equipment.
		/// </summary>
		public ControlRequestConnectCallback RequestConnectCallback { get; set; }

		/// <summary>
		/// Raised when Deinitialize is called. Typically used by the parent
		/// crosspoint manager to close an existing network connection to equipment.
		/// </summary>
		public ControlRequestDisconnectCallback RequestDisconnectCallback { get; set; }

		/// <summary>
		/// Gets the id of the equipment crosspoint that this control is currently
		/// communicating with.
		/// </summary>
		public int EquipmentCrosspoint
		{
			get { return m_EquipmentCrosspoint; }
			private set
			{
				if (value == m_EquipmentCrosspoint)
					return;

				m_EquipmentCrosspoint = value;

				Logger.AddEntry(eSeverity.Informational, "{0} connected equipment changed to {1}", this, m_EquipmentCrosspoint);
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="name"></param>
		public ControlCrosspoint(int id, string name)
			: base(id, name)
		{
			m_SigCache = new SigCache();
			m_SigCacheSection = new SafeCriticalSection();
			m_InitializeSection = new SafeCriticalSection();
			m_SigsWaitingToBeCleared = false;

			EquipmentCrosspoint = Xp3Utils.NULL_EQUIPMENT;
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public override void Dispose()
		{
			RequestConnectCallback = null;
			RequestDisconnectCallback = null;

			base.Dispose();
		}

		/// <summary>
		/// Clears the cached sigs sent by XP3.
		/// I.e. clear any high states from previously connected equipment.
		/// </summary>
		[PublicAPI]
		public void ClearSigs()
		{
			CrosspointData data;

			m_SigCacheSection.Enter();

			try
			{
				data = CrosspointData.ControlClear(Id, EquipmentCrosspoint, m_SigCache);
				m_SigCache.Clear();
				m_SigsWaitingToBeCleared = false;
			}
			finally
			{
				m_SigCacheSection.Leave();
			}

			SendOutputData(data);
		}

		/// <summary>
		/// Override this method to handle data before it is sent to the program.
		/// </summary>
		/// <param name="data"></param>
		protected override void PreSendOutputData(CrosspointData data)
		{
			m_SigCacheSection.Enter();

			try
			{
				// Cache changed sigs for clear
				if (data != null)
				{
					if (m_SigsWaitingToBeCleared &&
					    (data.MessageType == CrosspointData.eMessageType.EquipmentConnect ||
					     data.MessageType == CrosspointData.eMessageType.Message))
					{
						SigInfo[] newSigs = data.GetSigs().ToArray();
						m_SigCache.RemoveRange(newSigs);
						data.AddSigs(m_SigCache);
						m_SigsWaitingToBeCleared = false;
						m_SigCache.Clear();
						m_SigCache.AddHighClearRemoveLow(newSigs);
					}
					else
						m_SigCache.AddHighClearRemoveLow(data.GetSigs());
				}
			}
			finally
			{
				m_SigCacheSection.Leave();
			}

			base.PreSendOutputData(data);
		}

		/// <summary>
		/// Deinitializes from the previous equipment, calls RequestConnectCallback and sends some initial joins.
		/// If equipment id is 0 (null equipment) no connection is attempted.
		/// </summary>
		/// <param name="equipmentId">The id of the target EquipmentCrosspoint.</param>
		/// <returns>True if the initialization, including connection, was successful. False if already initialized with the equipment.</returns>
		public bool Initialize(int equipmentId)
		{
			m_InitializeSection.Enter();

			try
			{
				if (equipmentId == EquipmentCrosspoint)
					return false;

				// If we are currently initialized, or we specifically want to deninit, attempt to deinit from the previous equipment.
				if (EquipmentCrosspoint != Xp3Utils.NULL_EQUIPMENT || equipmentId == Xp3Utils.NULL_EQUIPMENT)
				{
					bool deinit = Deinitialize(equipmentId == Xp3Utils.NULL_EQUIPMENT);

					// If deinit failed, or equipment id is null equipment (i.e. we just wanted to deinit) return the value.
					if (deinit == false || equipmentId == Xp3Utils.NULL_EQUIPMENT)
						return deinit;
				}

				// Attempt to connect, return false if connection failed.
				ControlRequestConnectCallback callback = RequestConnectCallback;
				if (callback == null)
					return false;

				eCrosspointStatus connectStatus = callback(this, equipmentId);
				if (connectStatus != eCrosspointStatus.Connected)
					ClearSigs();

				EquipmentCrosspoint = connectStatus == eCrosspointStatus.Connected ? equipmentId : Xp3Utils.NULL_EQUIPMENT;
				Status = connectStatus;

				return connectStatus == eCrosspointStatus.Connected;
			}
			finally
			{
				m_InitializeSection.Leave();
			}
		}

		/// <summary>
		/// Performs some cleanup action and calls RequestDisconnectCallback.
		/// </summary>
		/// <returns></returns>
		public bool Deinitialize()
		{
			return Deinitialize(true);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Performs some cleanup action and calls RequestDisconnectCallback.
		/// </summary>
		/// <returns></returns>
		private bool Deinitialize(bool clearSigsOnDisconnect)
		{
			m_InitializeSection.Enter();

			try
			{
				// Allow the manager to determine disconnect before we decide to bail out.
				// This allows disconnection if the crosspoint has become desynchronized for some reason.
				ControlRequestDisconnectCallback callback = RequestDisconnectCallback;
				if (callback != null)
					Status = callback(this);
				else if (EquipmentCrosspoint == Xp3Utils.NULL_EQUIPMENT)
					return false;

				if (clearSigsOnDisconnect)
					ClearSigs();
				else
					m_SigsWaitingToBeCleared = true;

				// Successful disconnect
				EquipmentCrosspoint = Xp3Utils.NULL_EQUIPMENT;
				return true;
			}
			finally
			{
				m_InitializeSection.Leave();
			}
		}

		/// <summary>
		/// Gets the source control or the destination controls for a message originating from this crosspoint.
		/// </summary>
		/// <returns></returns>
		protected override IEnumerable<int> GetControlsForMessage()
		{
			yield return Id;
		}

		/// <summary>
		/// Gets the source equipment or destination equipment for a message originating from this crosspoint.
		/// </summary>
		/// <returns></returns>
		protected override int GetEquipmentForMessage()
		{
			return EquipmentCrosspoint;
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

			addRow("Connected Equipment", EquipmentCrosspoint);
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
				new GenericConsoleCommand<int>("Connect", "Connect <id> - Connects to the given equipment crosspoint",
				                               id => Initialize(id));

			yield return
				new ConsoleCommand("Disconnect", "Disconnects from the current equipment crosspoint", () => Deinitialize());
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		protected override string PrintSigs()
		{
			return PrintSigs(m_SigCache);
		}

		#endregion
	}
}
