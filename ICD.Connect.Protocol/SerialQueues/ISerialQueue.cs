using System;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.SerialQueues
{
	public interface ISerialQueue : IDisposable
	{
		/// <summary>
		/// Raised when serial data is sent to the port.
		/// </summary>
		event EventHandler<SerialTransmissionEventArgs> OnSerialTransmission;

		/// <summary>
		/// Raises individual commands with their responses.
		/// </summary>
		event EventHandler<SerialResponseEventArgs> OnSerialResponse;

		/// <summary>
		/// Raised when a command does not yield a response within a time limit.
		/// </summary>
		event EventHandler<SerialDataEventArgs> OnTimeout;

		/// <summary>
		/// Returns the number of queued commands.
		/// </summary>
		int CommandCount { get; }

		/// <summary>
		/// Gets the number of times in a row the queue has raised a timeout.
		/// </summary>
		int TimeoutCount { get; }

		/// <summary>
		/// Gets/sets the number of times to timeout in a row before clearing the queue.
		/// </summary>
		int MaxTimeoutCount { get; set; }

		/// <summary>
		/// Wait time between sending commands, defaults to 0.
		/// </summary>
		long CommandDelayTime { get; set; }

		/// <summary>
		/// Gets the current port.
		/// </summary>
		ISerialPort Port { get; }

		/// <summary>
		/// When true the serial queue will ignore responses and immediately start processing the next command.
		/// </summary>
		bool Trust { get; set; }

		/// <summary>
		/// Clears the command queue.
		/// </summary>
		void Clear();

		/// <summary>
		/// Queues data to be sent.
		/// </summary>
		/// <param name="data"></param>
		void Enqueue(ISerialData data);

		/// <summary>
		/// Uses the comparer to determine if a matching command is already queued.
		/// If true, replace the original command with the new one.
		/// 
		/// This is useful in cases such as ramping volume, where we can collapse:
		///		PowerOn
		///		Volume11
		///		Volume12
		///		MuteOn
		///		Volume13
		/// 
		/// To:
		///		PowerOn
		///		Volume13
		///		MuteOn
		/// </summary>
		/// <param name="data"></param>
		/// <param name="comparer"></param>
		void Enqueue(ISerialData data, Func<ISerialData, ISerialData, bool> comparer);

		/// <summary>
		/// Enqueues the given data at higher than normal priority.
		/// </summary>
		/// <param name="data"></param>
		void EnqueuePriority(ISerialData data);

		/// <summary>
		/// Enqueues the given data with the given priority (lower value is higher priority) 
		/// </summary>
		/// <param name="data"></param>
		/// <param name="priority"></param>
		void EnqueuePriority(ISerialData data, int priority);

		///  <summary>
		///  Enqueues the given data with the given priority (lower value is higher priority) 
		/// 
		///  Uses the comparer to determine if a matching command is already queued.
		///  If true, replace the original command with the new one.
		///  
		///  This is useful in cases such as ramping volume, where we can collapse:
		/// 		PowerOn
		/// 		Volume11
		/// 		Volume12
		/// 		MuteOn
		/// 		Volume13
		///  
		///  To:
		/// 		PowerOn
		/// 		Volume13
		/// 		MuteOn
		///  </summary>
		///  <param name="data"></param>
		///  <param name="comparer"></param>
		///  <param name="priority"></param>
		///  <param name="deDuplicateToEndOfQueue"></param>
		void EnqueuePriority(ISerialData data, Func<ISerialData, ISerialData, bool> comparer, int priority,
		                     bool deDuplicateToEndOfQueue);
	}
}
