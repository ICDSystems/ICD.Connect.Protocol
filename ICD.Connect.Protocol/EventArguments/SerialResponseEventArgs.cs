using System;
using ICD.Connect.Protocol.Data;

namespace ICD.Connect.Protocol.EventArguments
{
	/// <summary>
	/// SerialResponseEventArgs describes the recieved data from a serial port.
	/// </summary>
	public sealed class SerialResponseEventArgs : EventArgs
	{
		/// <summary>
		/// The data sent to the device
		/// </summary>
		public ISerialData Data { get; private set; }

		/// <summary>
		/// The response from the device
		/// </summary>
		public string Response { get; private set; }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="response"></param>
		public SerialResponseEventArgs(ISerialData data, string response)
		{
			Data = data;
			Response = response;
		}
	}
}
