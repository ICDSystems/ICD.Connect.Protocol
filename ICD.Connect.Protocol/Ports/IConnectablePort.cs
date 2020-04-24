using System;
using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Protocol.Ports
{
	public interface IConnectablePort : IPort
	{
		/// <summary>
		/// Raised when the port connection status changes.
		/// </summary>
		event EventHandler<BoolEventArgs> OnConnectedStateChanged;

		#region Properties

		/// <summary>
		/// Gets the current connection status of the port.
		/// </summary>
		bool IsConnected { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Connects the port.
		/// </summary>
		void Connect();

		/// <summary>
		/// Disconnects the port.
		/// </summary>
		void Disconnect();

		#endregion
	}
}