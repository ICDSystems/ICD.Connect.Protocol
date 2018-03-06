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
		private readonly SafeCriticalSection m_ProcessSection;
		private readonly List<T> m_FeedbackQueue;
		private readonly IEqualityComparer<T> m_EqualityComparer;

		private T m_MostRecentValue;

		public FeedbackDebounce()
			: this(EqualityComparer<T>.Default)
		{
		}

		public FeedbackDebounce(IEqualityComparer<T> equalityComparer)
		{
			m_QueueSection = new SafeCriticalSection();
			m_ProcessSection = new SafeCriticalSection();
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
			m_ProcessSection.Enter();

			try
			{
				T newValue;
				if (!CollapseQueue(out newValue))
					return;

				OnValue.Raise(this, new GenericEventArgs<T>(newValue));
			}
			finally
			{
				m_ProcessSection.Leave();
			}
		}

		private bool CollapseQueue(out T newValue)
		{
			newValue = default(T);

			try
			{
				m_QueueSection.Enter();

				if (m_FeedbackQueue.Count == 0 ||
				    m_EqualityComparer.Equals(m_FeedbackQueue[m_FeedbackQueue.Count - 1], m_MostRecentValue))
				{
					m_FeedbackQueue.Clear();
					return false;
				}

				int index = -1;

				for (int i = m_FeedbackQueue.Count - 1; i >= 0; i--)
				{
					if (!m_EqualityComparer.Equals(m_FeedbackQueue[i], m_MostRecentValue))
						continue;
					index = i;
					break;
				}

				newValue = m_FeedbackQueue[index + 1];
				m_MostRecentValue = newValue;

				m_FeedbackQueue.RemoveRange(0, index + 1);

				return true;
			}
			finally
			{
				m_QueueSection.Leave();
			}
		}
	}
}
