using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Sigs;

namespace ICD.Connect.Protocol.Crosspoints.Crosspoints
{
	public abstract class AbstractEquipmentCrosspoint : AbstractCrosspoint, IEquipmentCrosspoint
	{
		public event EventHandler<IntEventArgs> OnControlCrosspointCountChanged;

		public event EventHandler<IntEventArgs> OnControlCrosspointConnected;

		public event EventHandler<IntEventArgs> OnControlCrosspointDisconnected;

		private readonly IcdHashSet<int> m_ControlCrosspoints;
		private readonly SafeCriticalSection m_ControlCrosspointsSection;

		/// <summary>
		/// SmartObject -> Number -> ControlId
		/// Tracks the digital sigs coming from controls to this equipment so we can clear button hold states on disconnect.
		/// </summary>
		private readonly Dictionary<ushort, Dictionary<uint, IcdHashSet<int>>> m_ControlDigitalSigs;

		#region Properties

		/// <summary>
		/// Gets the ids for the control crosspoints that are currently connected to this equipment.
		/// </summary>
		public IEnumerable<int> ControlCrosspoints
		{
			get { return m_ControlCrosspointsSection.Execute(() => m_ControlCrosspoints.Order().ToArray()); }
		}

		/// <summary>
		/// Gets the number of connected control crosspoints.
		/// </summary>
		public int ControlCrosspointsCount
		{
			get { return m_ControlCrosspointsSection.Execute(() => m_ControlCrosspoints.Count); }
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="name"></param>
		protected AbstractEquipmentCrosspoint(int id, string name)
			: base(id, name)
		{
			m_ControlCrosspoints = new IcdHashSet<int>();
			m_ControlCrosspointsSection = new SafeCriticalSection();

			m_ControlDigitalSigs = new Dictionary<ushort, Dictionary<uint, IcdHashSet<int>>>();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public override void Dispose()
		{
			OnControlCrosspointCountChanged = null;
			OnControlCrosspointConnected = null;
			OnControlCrosspointDisconnected = null;

			base.Dispose();
		}

		#region Methods

		/// <summary>
		/// Typically called once a control has connected to this equipment, Initialize
		/// sets some initial values on the given control.
		/// </summary>
		/// <param name="controlId"></param>
		public void Initialize(int controlId)
		{
			CrosspointData connect;

			m_ControlCrosspointsSection.Enter();

			try
			{
				if (!m_ControlCrosspoints.Add(controlId))
					return;

				Logger.AddEntry(eSeverity.Informational, "{0} added connected control {1}", this, controlId);

				OnControlCrosspointCountChanged.Raise(this, new IntEventArgs(ControlCrosspointsCount));
				if (ControlCrosspointsCount > 0)
					Status = eCrosspointStatus.Connected;

				connect = GetConnectCrosspointData(controlId);
			}
			finally
			{
				m_ControlCrosspointsSection.Leave();
			}

			SendInputData(connect);

			OnControlCrosspointConnected.Raise(this, new IntEventArgs(controlId));
		}

		/// <summary>
		/// Removes the control from the internal collection of control crosspoints.
		/// </summary>
		/// <param name="controlId"></param>
		public void Deinitialize(int controlId)
		{
			CrosspointData disconnect;

			m_ControlCrosspointsSection.Enter();

			try
			{
				if (!RemoveControlCrosspoint(controlId))
					return;

				Logger.AddEntry(eSeverity.Informational, "{0} removed connected control {1}", this, controlId);

				OnControlCrosspointCountChanged.Raise(this, new IntEventArgs(ControlCrosspointsCount));
				if (ControlCrosspointsCount <= 0)
					Status = eCrosspointStatus.Idle;

				disconnect = CrosspointData.EquipmentDisconnect(controlId, Id);
			}
			finally
			{
				m_ControlCrosspointsSection.Leave();
			}

			SendInputData(disconnect);

			OnControlCrosspointDisconnected.Raise(this, new IntEventArgs(controlId));
		}

		/// <summary>
		/// Removes the control with the given id from the caches.
		/// </summary>
		/// <param name="controlId"></param>
		/// <returns></returns>
		private bool RemoveControlCrosspoint(int controlId)
		{
			CrosspointData data;

			m_ControlCrosspointsSection.Enter();

			try
			{
				if (!m_ControlCrosspoints.Remove(controlId))
					return false;

				// Remove the cached bools so we can release any held buttons
				List<SigInfo> digitals = new List<SigInfo>();

				foreach (KeyValuePair<ushort, Dictionary<uint, IcdHashSet<int>>> soKvp in m_ControlDigitalSigs)
				{
					foreach (var numKvp in soKvp.Value)
					{
						if (numKvp.Value.Remove(controlId) && numKvp.Value.Count == 0)
							digitals.Add(new SigInfo(numKvp.Key, soKvp.Key, true));
					}
				}

				if (digitals.Count == 0)
					return true;

				data = CrosspointData.EquipmentClear(controlId, Id, digitals);
			}
			finally
			{
				m_ControlCrosspointsSection.Leave();
			}

			SendOutputData(data);

			return true;
		}

		/// <summary>
		/// Disconnects the equipment from all currently connected controls.
		/// </summary>
		public void Deinitialize()
		{
			int[] controls = ControlCrosspoints.ToArray();
			foreach (int controlId in controls)
				Deinitialize(controlId);
		}

		protected virtual CrosspointData GetConnectCrosspointData(int controlId)
		{
			return CrosspointData.EquipmentConnect(controlId, Id);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Override this method to handle data before it is sent to the program.
		/// </summary>
		/// <param name="data"></param>
		protected override void PreSendOutputData(CrosspointData data)
		{
			base.PreSendOutputData(data);

			CacheDigitalSigs(data);
		}

		/// <summary>
		/// Cache digital sigs from the control crosspoint so we can clear on disconnect.
		/// </summary>
		/// <param name="data"></param>
		private void CacheDigitalSigs(CrosspointData data)
		{
			m_ControlCrosspointsSection.Enter();

			try
			{
				int[] controlIds = data.GetControlIds().ToArray();
				if (controlIds.Length == 0)
					return;

				foreach (SigInfo item in data.GetSigs().Where(s => s.Type == eSigType.Digital))
				{
					IcdHashSet<int> cachedControlIds =
						m_ControlDigitalSigs.GetOrAddNew(item.SmartObject).GetOrAddNew(item.Number);

					if (item.GetBoolValue())
						cachedControlIds.AddRange(controlIds);
					else
						cachedControlIds.RemoveRange(controlIds);
				}
			}
			finally
			{
				m_ControlCrosspointsSection.Leave();
			}
		}

		/// <summary>
		/// Gets the source control or the destination controls for a message originating from this crosspoint.
		/// </summary>
		/// <returns></returns>
		protected override IEnumerable<int> GetControlsForMessage()
		{
			return m_ControlCrosspointsSection.Execute(() => m_ControlCrosspoints.ToArray());
		}

		/// <summary>
		/// Gets the source equipment or destination equipment for a message originating from this crosspoint.
		/// </summary>
		/// <returns></returns>
		protected override int GetEquipmentForMessage()
		{
			return Id;
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

			addRow("Connected Equipment", StringUtils.ArrayFormat(ControlCrosspoints));
		}

		#endregion
	}
}
