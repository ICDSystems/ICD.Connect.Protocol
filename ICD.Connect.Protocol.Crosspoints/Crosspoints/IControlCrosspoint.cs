using ICD.Common.Properties;

namespace ICD.Connect.Protocol.Crosspoints.Crosspoints
{
	public delegate eCrosspointStatus ControlRequestConnectCallback(IControlCrosspoint sender, int equipmentId);

	public delegate eCrosspointStatus ControlRequestDisconnectCallback(IControlCrosspoint sender);

	public interface IControlCrosspoint : ICrosspoint
	{
		/// <summary>
		/// Raised when Initialize is called. Typically used by the parent
		/// crosspoint manager to establish a network connection to the equipment.
		/// </summary>
		[PublicAPI]
		ControlRequestConnectCallback RequestConnectCallback { get; set; }

		/// <summary>
		/// Raised when Deinitialize is called. Typically used by the parent
		/// crosspoint manager to close an existing network connection to equipment.
		/// </summary>
		[PublicAPI]
		ControlRequestDisconnectCallback RequestDisconnectCallback { get; set; }

		/// <summary>
		/// Gets the id of the equipment crosspoint that this control is currently
		/// communicating with.
		/// </summary>
		[PublicAPI]
		int EquipmentCrosspoint { get; }

		/// <summary>
		/// Calls RequestConnectCallback and sends some initial joins.
		/// </summary>
		/// <param name="equipmentId">The id of the target EquipmentCrosspoint.</param>
		/// <returns>True if the initialization, including connection, was successful.</returns>
		[PublicAPI]
		bool Initialize(int equipmentId);

		/// <summary>
		/// Performs some cleanup action and calls RequestDisconnectCallback.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		bool Deinitialize();
	}
}
