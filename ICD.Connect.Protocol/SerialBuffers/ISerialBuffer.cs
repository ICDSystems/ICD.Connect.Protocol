using System;
using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Protocol.SerialBuffers
{
	/// <summary>
	/// ISerialBuffers store serial data until a complete string can be returned.
	/// </summary>
	public interface ISerialBuffer
	{
		/// <summary>
		/// Raised when a complete message has been buffered.
		/// </summary>
		event EventHandler<StringEventArgs> OnCompletedSerial;

		/// <summary>
		/// Enqueues the serial data.
		/// </summary>
		/// <param name="data"></param>
		void Enqueue(string data);

		/// <summary>
		/// Clears all queued data in the buffer.
		/// </summary>
		void Clear();
	}
}
