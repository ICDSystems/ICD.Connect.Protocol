using System;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.SerialQueues
{
	public interface ISerialQueue : IDisposable
	{
		event EventHandler<SerialResponseEventArgs> OnSerialResponse;

		event EventHandler<SerialDataEventArgs> OnTimeout;

		/// <summary>
		/// Gets the current port.
		/// </summary>
		ISerialPort Port { get; }

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
		void Enqueue<T>(T data, Func<T, T, bool> comparer) where T : ISerialData;

		void EnqueuePriority(ISerialData data);
	}

	public static class SerialQueueExtensions
	{
		/// <summary>
		/// Queues data to be sent.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="data"></param>
		public static void Enqueue(this ISerialQueue extends, string data)
		{
			extends.Enqueue(new SerialData(data));
		}

		public static void EnqueuePriority(this ISerialQueue extends, string data)
		{
			extends.EnqueuePriority(new SerialData(data));
		}
	}
}
