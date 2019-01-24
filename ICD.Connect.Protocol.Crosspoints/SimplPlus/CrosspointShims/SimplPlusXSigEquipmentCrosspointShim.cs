using ICD.Common.Utils.EventArguments;
using ICD.Common.Properties;
using ICD.Connect.Protocol.Crosspoints.CrosspointManagers;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;

namespace ICD.Connect.Protocol.Crosspoints.SimplPlus.CrosspointShims
{
	public sealed class SimplPlusXSigEquipmentCrosspointShim : AbstractSimplPlusXSigCrosspointShim<IEquipmentCrosspoint>
	{
		#region Callbacks

		[PublicAPI("S+")]
		public SPlusCountCallback ControlCrosspointsConnectedCallback { get; set; }

		#endregion

		#region Public Properties

		protected override ICrosspointManager Manager
		{
			get { return System == null ? null : System.GetOrCreateEquipmentCrosspointManager(); }
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
			if (Crosspoint == null)
				return;

			Crosspoint.Deinitialize(controlId);
		}

		[PublicAPI("S+")]
		public override void Disconnect()
		{
			//Don't do things without a crosspoint
			if (Crosspoint == null)
				return;

			Crosspoint.Deinitialize();
		}

		#endregion

		#region Protected / Private Methods

		protected override void RegisterCrosspoint()
		{
			if (Crosspoint == null)
				return;

			Crosspoint.OnSendOutputData += CrosspointOnSendOutputData;
			Crosspoint.OnStatusChanged += CrosspointOnStatusChanged;
			Crosspoint.OnControlCrosspointCountChanged += CrosspointOnControlCrosspointCountChanged;

			SPlusStatusUpdateCallback statusCallback = CrosspointStatusCallback;
			if (statusCallback != null)
				statusCallback((ushort)Crosspoint.Status);

			SPlusCountCallback connectionsCallback = ControlCrosspointsConnectedCallback;
			if (connectionsCallback != null)
				connectionsCallback((ushort)Crosspoint.ControlCrosspointsCount);

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
			Crosspoint.OnControlCrosspointCountChanged -= CrosspointOnControlCrosspointCountChanged;

			SPlusStatusUpdateCallback statusCallback = CrosspointStatusCallback;
			if (statusCallback != null)
				statusCallback(0);

			SPlusCountCallback connectedControlsCallback = ControlCrosspointsConnectedCallback;
			if (connectedControlsCallback != null)
				connectedControlsCallback(0);

			SPlusCrosspointChangedCallback crosspointChangedCallback = CrosspointChangedCallback;
			if (crosspointChangedCallback != null)
				crosspointChangedCallback();
		}

		protected override IEquipmentCrosspoint CreateCrosspoint(int id, string name)
		{
			return new EquipmentCrosspoint(id, name);
		}

		private void CrosspointOnControlCrosspointCountChanged(object sender, IntEventArgs args)
		{
			SPlusCountCallback callback = ControlCrosspointsConnectedCallback;
			if (callback != null)
				callback((ushort)args.Data);
		}
		
		#endregion
	}
}
