using ICD.Common.Utils.EventArguments;
using ICD.Common.Properties;
using ICD.Connect.Protocol.Crosspoints.CrosspointManagers;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;

namespace ICD.Connect.Protocol.Crosspoints.SimplPlus.CrosspointShims
{
	public sealed class SimplPlusXSigEquipmentCrosspointShim : AbstractSimplPlusXSigCrosspointShim
	{
		#region Private Members

		private EquipmentCrosspointManager m_Manager;
		private EquipmentCrosspoint m_Crosspoint;

		#endregion

		#region Public Properties

		public override ICrosspoint Crosspoint
		{
			get
			{
				return m_Crosspoint;
			}
			protected set
			{
				EquipmentCrosspoint crosspoint = value as EquipmentCrosspoint;
				if (crosspoint != null)
					m_Crosspoint = crosspoint;
			}
		}

		protected override ICrosspointManager Manager
		{
			get
			{
				return m_Manager;
			}
			set
			{
				EquipmentCrosspointManager manager = value as EquipmentCrosspointManager;
				if (manager != null)
					m_Manager = manager;
			}
		}

		#endregion

		/// <summary>
		/// Default constructor for S+.
		/// </summary>
		[UsedImplicitly]
		public SimplPlusXSigEquipmentCrosspointShim() { }

		#region Public S+ Methods

		[PublicAPI("S+")]
		public void DisconnectFromControl(int controlId)
		{
			//Don't do things without a crosspoint
			if (m_Crosspoint == null)
				return;

			m_Crosspoint.Deinitialize(controlId);
		}

		[PublicAPI("S+")]
		public override void Disconnect()
		{
			//Don't do things without a crosspoint
			if (m_Crosspoint == null)
				return;

			m_Crosspoint.Deinitialize();
		}

		#endregion

		#region Protected / Private Methods

		protected override void RegisterCrosspoint()
		{
			m_Crosspoint.OnSendOutputData += CrosspointOnSendOutputData;
			m_Crosspoint.OnStatusChanged += CrosspointOnStatusChanged;
			m_Crosspoint.OnControlCrosspointCountChanged += CrosspointOnControlCrosspointCountChanged;

			SPlusStatusUpdateCallback statusCallback = CrosspointStatusCallback;
			if (statusCallback != null)
				statusCallback((ushort)m_Crosspoint.Status);

			SPlusStatusUpdateCallback connectionsCallback = CrosspointStatusCallback;
			if (connectionsCallback != null)
				connectionsCallback((ushort)m_Crosspoint.ControlCrosspointsCount);

			SPlusCrosspointChangedCallback crosspointChangedCallback = CrosspointChangedCallback;
			if (crosspointChangedCallback != null)
				crosspointChangedCallback();
		}

		protected override void UnregisterCrosspoint()
		{
			m_Crosspoint.OnSendOutputData -= CrosspointOnSendOutputData;
			m_Crosspoint.OnStatusChanged -= CrosspointOnStatusChanged;
			m_Crosspoint.OnControlCrosspointCountChanged -= CrosspointOnControlCrosspointCountChanged;

			SPlusStatusUpdateCallback statusCallback = CrosspointStatusCallback;
			if (statusCallback != null)
				statusCallback(0);

			SPlusStatusUpdateCallback connectedControlsCallback = CrosspointStatusCallback;
			if (connectedControlsCallback != null)
				connectedControlsCallback(0);

			SPlusCrosspointChangedCallback crosspointChangedCallback = CrosspointChangedCallback;
			if (crosspointChangedCallback != null)
				crosspointChangedCallback();
		}

		private void CrosspointOnControlCrosspointCountChanged(object sender, IntEventArgs args)
		{
			SPlusStatusUpdateCallback callback = CrosspointStatusCallback;
			if (callback != null)
				callback((ushort)args.Data);
		}
		
		#endregion
	}
}
