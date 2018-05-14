using System;
using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Protocol.Ports
{
	/// <summary>
	/// ISerialPort provides methods for sending and receiving serial data.
	/// </summary>
	public interface ISerialPort : IPort
	{
		/// <summary>
		/// Raised when data is received from the port.
		/// </summary>
		event EventHandler<StringEventArgs> OnSerialDataReceived;

		/// <summary>
		/// Raised when the port connection status changes.
		/// </summary>
		event EventHandler<BoolEventArgs> OnConnectedStateChanged;

		/// <summary>
		/// Gets the current connection status of the port.
		/// </summary>
		bool IsConnected { get; }

		/// <summary>
		/// Connects the port.
		/// </summary>
		void Connect();

		/// <summary>
		/// Disconnects the port.
		/// </summary>
		void Disconnect();

		/// <summary>
		/// Sends the data to the port.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		bool Send(string data);

		void Receive(string data);
	}
}
