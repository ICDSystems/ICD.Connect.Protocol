using System;
using ICD.Connect.Protocol.Data;

namespace ICD.Connect.Protocol.EventArguments
{
	public sealed class SerialTransmissionEventArgs : EventArgs
	{
		/// <summary>
		/// The data sent to the device
		/// </summary>
		public ISerialData Data { get; private set; }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public SerialTransmissionEventArgs(ISerialData data)
		{
			Data = data;
		}
	}
}
