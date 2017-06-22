using System;
using ICD.Common.EventArguments;

namespace ICD.Connect.Protocol.Ports
{
	/// <summary>
	/// ISerialPort provides methods for sending and receiving serial data.
	/// </summary>
	public interface ISerialPort : IPort
	{
		event EventHandler<StringEventArgs> OnSerialDataReceived;
		event EventHandler<BoolEventArgs> OnConnectedStateChanged;

		bool IsConnected { get; }

		/// <summary>
		/// Connects the port.
		/// </summary>
		void Connect();

		/// <summary>
		/// Disconnects the port.
		/// </summary>
		void Disconnect();

		bool Send(string data);

		void Receive(string data);
	}
}
