#if SIMPLSHARP
using ICD.Common.Properties;
using ICD.Connect.Protocol.Crosspoints.CrosspointManagers;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;

namespace ICD.Connect.Protocol.Crosspoints.SimplPlus.CrosspointShims
{
	public sealed class SimplPlusXSigControlCrosspointShim : AbstractSimplPlusXSigCrosspointShim
	{
		#region Private Members

		private ControlCrosspointManager m_Manager;
		private ControlCrosspoint m_Crosspoint;

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
				ControlCrosspoint crosspoint = value as ControlCrosspoint;
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
				ControlCrosspointManager manager = value as ControlCrosspointManager;
				if (manager != null)
					m_Manager = manager;
			}
		}

		#endregion

		/// <summary>
		/// Default constructor for S+.
		/// </summary>
		[UsedImplicitly]
		public SimplPlusXSigControlCrosspointShim() { }

		#region Public S+ Methods

		[PublicAPI("S+")]
		public void ConnectToEquipment(int equipmentId)
		{
			//Don't do things without a crosspoint
			if (m_Crosspoint == null)
				return;

			m_Crosspoint.Initialize(equipmentId);
		}

		[PublicAPI("S+")]
		public void DisconnectToEquipment(int equipmentId)
		{
			//Don't do things without a crosspoint
			if (m_Crosspoint == null)
				return;

			m_Crosspoint.Initialize(Xp3Utils.NULL_EQUIPMENT);
		}

		[PublicAPI("S+")]
		public override void Disconnect()
		{
			//Don't do things without a crosspoint
			if (m_Crosspoint == null)
				return;

			m_Crosspoint.Deinitialize();
		}

		/// <summary>
		/// When true, the crosspoint manager will attempt to reconnect when a connection is dropped.
		/// </summary>
		/// <param name="autoReconnect"></param>
		[PublicAPI("S+")]
		public void SetAutoReconnect(ushort autoReconnect)
		{
			if (m_Manager == null)
				return;

			m_Manager.AutoReconnect = autoReconnect != 0;
		}

		#endregion

		#region Protected / Private Methods

		protected override void RegisterCrosspoint()
		{
			m_Crosspoint.OnSendOutputData += CrosspointOnSendOutputData;
			m_Crosspoint.OnStatusChanged += CrosspointOnStatusChanged;

			SPlusStatusUpdateCallback statusCallback = CrosspointStatusCallback;
			if (statusCallback != null)
				statusCallback((ushort)m_Crosspoint.Status);

			SPlusCrosspointChangedCallback crosspointChangedCallback = CrosspointChangedCallback;
			if (crosspointChangedCallback != null)
				crosspointChangedCallback();
		}

		protected override void UnregisterCrosspoint()
		{
			m_Crosspoint.OnSendOutputData -= CrosspointOnSendOutputData;
			m_Crosspoint.OnStatusChanged -= CrosspointOnStatusChanged;

			SPlusStatusUpdateCallback statusCallback = CrosspointStatusCallback;
			if (statusCallback != null)
				statusCallback(0);

			SPlusCrosspointChangedCallback crosspointChangedCallback = CrosspointChangedCallback;
			if (crosspointChangedCallback != null)
				crosspointChangedCallback();
		}

		#endregion
	}
}

#endif
