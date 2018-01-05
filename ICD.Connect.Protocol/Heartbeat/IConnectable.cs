using System;
using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Protocol.Heartbeat
{
	public interface IConnectable
	{
		/// <summary>
		/// Raised when the connected state changes.
		/// </summary>
		event EventHandler<BoolEventArgs> OnConnectedStateChanged;

		/// <summary>
		/// Gets the current connected state of the instance.
		/// </summary>
		bool IsConnected { get; }

		/// <summary>
		/// Gets the heartbeat instance that is enforcing the connection state.
		/// </summary>
		Heartbeat Heartbeat { get; }

		/// <summary>
		/// Connect the instance to the remote endpoint.
		/// </summary>
		void Connect();

		/// <summary>
		/// Disconnects the instance from the remote endpoint.
		/// </summary>
		void Disconnect();
	}
}
