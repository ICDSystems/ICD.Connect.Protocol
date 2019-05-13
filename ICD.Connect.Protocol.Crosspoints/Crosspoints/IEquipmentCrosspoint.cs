using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Protocol.Crosspoints.Crosspoints
{
	public interface IEquipmentCrosspoint : ICrosspoint
	{
		event EventHandler<IntEventArgs> OnControlCrosspointCountChanged;

		event EventHandler<IntEventArgs> OnControlCrosspointConnected;

		event EventHandler<IntEventArgs> OnControlCrosspointDisconnected;

		/// <summary>
		/// Gets the ids for the control crosspoints that are currently connected to this equipment.
		/// </summary>
		[PublicAPI]
		IEnumerable<int> ControlCrosspoints { get; }

		/// <summary>
		/// Gets the number of connected control crosspoints.
		/// </summary>
		[PublicAPI]
		int ControlCrosspointsCount { get; }

		/// <summary>
		/// Typically called once a control has connected to this equipment, Initialize
		/// sets some initial values on the given control.
		/// </summary>
		/// <param name="controlId"></param>
		[PublicAPI]
		void Initialize(int controlId);

		/// <summary>
		/// Removes the control from the internal collection of control crosspoints.
		/// </summary>
		/// <param name="controlId"></param>
		[PublicAPI]
		void Deinitialize(int controlId);

		/// <summary>
		/// Disconnects the equipment from all currently connected controls.
		/// </summary>
		[PublicAPI]
		void Deinitialize();
	}
}
