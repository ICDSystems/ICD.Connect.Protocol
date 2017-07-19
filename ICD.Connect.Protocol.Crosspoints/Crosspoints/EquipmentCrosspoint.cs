using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.EventArguments;
using ICD.Connect.API.Nodes;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.Sigs;

namespace ICD.Connect.Protocol.Crosspoints.Crosspoints
{
	/// <summary>
	/// EquipmentCrosspoints typically represent a piece of hardware like
	/// a lighting system, or HVAC.
	/// </summary>
	public sealed class EquipmentCrosspoint : AbstractCrosspoint, IEquipmentCrosspoint
	{
		private readonly IcdHashSet<int> m_ControlCrosspoints;
		private readonly SafeCriticalSection m_ControlCrosspointsSection;

		// We cache changed sigs to send to controls on connect.
		private readonly SigCache m_SigCache;
		private readonly SafeCriticalSection m_SigCacheSection;

		public event EventHandler<IntEventArgs> OnControlCrosspointCountChanged;

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
		public EquipmentCrosspoint(int id, string name)
			: base(id, name)
		{
			m_ControlCrosspoints = new IcdHashSet<int>();
			m_ControlCrosspointsSection = new SafeCriticalSection();

			m_SigCache = new SigCache();
			m_SigCacheSection = new SafeCriticalSection();
		}

		#region Methods

		/// <summary>
		/// Override this method to handle data before it is sent to XP3.
		/// </summary>
		/// <param name="data"></param>
		protected override void PreSendInputData(CrosspointData data)
		{
			m_SigCacheSection.Enter();

			try
			{
				// Cache sigs to send to controls on connect
				if (data != null)
					m_SigCache.AddHighRemoveLow(data.GetSigs());
			}
			finally
			{
				m_SigCacheSection.Leave();
			}

			base.PreSendInputData(data);
		}

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
				OnControlCrosspointCountChanged.Raise(this, new IntEventArgs(ControlCrosspointsCount));

				connect = CrosspointData.EquipmentConnect(controlId, Id, m_SigCache);
			}
			finally
			{
				m_ControlCrosspointsSection.Leave();
			}

			SendInputData(connect);
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
				if (!m_ControlCrosspoints.Remove(controlId))
					return;

				disconnect = CrosspointData.EquipmentDisconnect(controlId, Id);
			}
			finally
			{
				m_ControlCrosspointsSection.Leave();
			}

			SendInputData(disconnect);
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

		#endregion

		#region Private Methods

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
