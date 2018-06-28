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
		/// Rasied when the port receives data from the remote endpoint.
		/// </summary>
		event EventHandler<StringEventArgs> OnSerialDataReceived;

		/// <summary>
		/// Raised when the port connects/disconnects to/from the remote endpoint.
		/// </summary>
		event EventHandler<BoolEventArgs> OnConnectedStateChanged;

		/// <summary>
		/// Gets the current connection satte.
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
		/// Sends data to the remote endpoint.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		bool Send(string data);

		/// <summary>
		/// Simulates receiving data from the remote endpoint.
		/// </summary>
		/// <param name="data"></param>
		void Receive(string data);
	}
}
