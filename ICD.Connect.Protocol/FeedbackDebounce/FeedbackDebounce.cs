using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Protocol.FeedbackDebounce
{
	public sealed class FeedbackDebounce<T> : IFeedbackDebounce<T>
	{
		public event EventHandler<GenericEventArgs<T>> OnValue;

		private readonly SafeCriticalSection m_QueueSection;
		private bool m_Processing;
		private readonly List<T> m_FeedbackQueue;
		private readonly IEqualityComparer<T> m_EqualityComparer;

		private T m_MostRecentValue;

		/// <summary>
		/// Constructor.
		/// </summary>
		public FeedbackDebounce()
			: this(EqualityComparer<T>.Default)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="equalityComparer"></param>
		public FeedbackDebounce(IEqualityComparer<T> equalityComparer)
		{
			m_QueueSection = new SafeCriticalSection();
			m_Processing = false;
			m_FeedbackQueue = new List<T>();

			m_EqualityComparer = equalityComparer;
		}

		public void Enqueue(T item)
		{
			m_QueueSection.Execute(() => m_FeedbackQueue.Add(item));
			ProcessQueueAsync();
		}

		private void ProcessQueueAsync()
		{
			ThreadingUtils.SafeInvoke(ProcessQueue);
		}


		private void ProcessQueue()
		{
			T newValue;

			// If we're not already processing, and we can get a new value, start processing
			m_QueueSection.Enter();
			try
			{
				if (m_Processing || !CollapseQueue(out newValue))
					return;

				m_Processing = true;
			}
			finally
			{
				m_QueueSection.Leave();
			}

			// Continue processing as long as we continue to get a new value
			while (true)
			{
				m_MostRecentValue = newValue;
				OnValue.Raise(this, new GenericEventArgs<T>(newValue));

				m_QueueSection.Enter();
				try
				{
					// If we get a new value, continue processing
					if (CollapseQueue(out newValue))
						continue;

					// If we don't get a new value, stop processing and return
					m_Processing = false;
					return;
				}
				finally
				{
					m_QueueSection.Leave();
				}
			}
		}

		/// <summary>
		/// Returns true if there is a difference between the last raised value and the most recent enqueued value.
		/// </summary>
		/// <param name="newValue"></param>
		/// <returns></returns>
		private bool CollapseQueue(out T newValue)
		{
			newValue = default(T);

			try
			{
				m_QueueSection.Enter();

				// No new items to process
				if (m_FeedbackQueue.Count == 0)
					return false;

				T lastValue = m_FeedbackQueue[m_FeedbackQueue.Count - 1];
				bool equals = m_EqualityComparer.Equals(lastValue, m_MostRecentValue);

				// Latest value is different to the last raised value
				if (!equals)
					newValue = lastValue;

				m_FeedbackQueue.Clear();
				return !equals;
			}
			finally
			{
				m_QueueSection.Leave();
			}
		}
	}
}
