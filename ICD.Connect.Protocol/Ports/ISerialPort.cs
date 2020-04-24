using System;
using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Protocol.Ports
{
	/// <summary>
	/// ISerialPort provides methods for sending and receiving serial data.
	/// </summary>
	public interface ISerialPort : IConnectablePort
	{
		/// <summary>
		/// Rasied when the port receives data from the remote endpoint.
		/// </summary>
		event EventHandler<StringEventArgs> OnSerialDataReceived;

		#region Methods

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

		#endregion
	}
}
