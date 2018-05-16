#if SIMPLSHARP
using ICD.Common.Properties;
using ICD.Connect.Protocol.Crosspoints.CrosspointManagers;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;

namespace ICD.Connect.Protocol.Crosspoints.SimplPlus.CrosspointShims
{
	public sealed class SimplPlusXSigControlCrosspointShim : AbstractSimplPlusXSigCrosspointShim<IControlCrosspoint>
	{
		#region Private Members

		private ControlCrosspointManager m_Manager;

		#endregion

		#region Public Properties

		public override IControlCrosspoint Crosspoint { get; protected set; }

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
			if (Crosspoint == null)
				return;

			Crosspoint.Initialize(equipmentId);
		}

		[PublicAPI("S+")]
		public void DisconnectToEquipment(int equipmentId)
		{
			//Don't do things without a crosspoint
			if (Crosspoint == null)
				return;

			Crosspoint.Initialize(Xp3Utils.NULL_EQUIPMENT);
		}

		[PublicAPI("S+")]
		public override void Disconnect()
		{
			//Don't do things without a crosspoint
			if (Crosspoint == null)
				return;

			Crosspoint.Deinitialize();
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
			Crosspoint.OnSendOutputData += CrosspointOnSendOutputData;
			Crosspoint.OnStatusChanged += CrosspointOnStatusChanged;

			SPlusStatusUpdateCallback statusCallback = CrosspointStatusCallback;
			if (statusCallback != null)
				statusCallback((ushort)Crosspoint.Status);

			SPlusCrosspointChangedCallback crosspointChangedCallback = CrosspointChangedCallback;
			if (crosspointChangedCallback != null)
				crosspointChangedCallback();
		}

		protected override void UnregisterCrosspoint()
		{
			Crosspoint.OnSendOutputData -= CrosspointOnSendOutputData;
			Crosspoint.OnStatusChanged -= CrosspointOnStatusChanged;

			SPlusStatusUpdateCallback statusCallback = CrosspointStatusCallback;
			if (statusCallback != null)
				statusCallback(0);

			SPlusCrosspointChangedCallback crosspointChangedCallback = CrosspointChangedCallback;
			if (crosspointChangedCallback != null)
				crosspointChangedCallback();
		}

		protected override IControlCrosspoint CreateCrosspoint(int id, string name)
		{
			return new ControlCrosspoint(CrosspointId, CrosspointName);
		}

		#endregion
	}
}

#endif
