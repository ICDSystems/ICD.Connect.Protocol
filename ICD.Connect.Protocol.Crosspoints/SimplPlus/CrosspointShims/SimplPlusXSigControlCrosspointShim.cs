using ICD.Common.Properties;
using ICD.Connect.Protocol.Crosspoints.CrosspointManagers;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;

namespace ICD.Connect.Protocol.Crosspoints.SimplPlus.CrosspointShims
{
	public sealed class SimplPlusXSigControlCrosspointShim : AbstractSimplPlusXSigCrosspointShim<IControlCrosspoint>
	{
		#region Public Properties

		protected override ICrosspointManager Manager
		{
			get { return System == null ? null : System.GetOrCreateControlCrosspointManager(); }
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
			ControlCrosspointManager controlCrosspointManager = Manager as ControlCrosspointManager;

			if (controlCrosspointManager != null)
				controlCrosspointManager.AutoReconnect = autoReconnect != 0;
		}

		#endregion

		#region Protected / Private Methods

		protected override void RegisterCrosspoint()
		{
			if (Crosspoint == null)
				return;

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
			if (Crosspoint == null)
				return;

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
