using System;
using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Protocol.FeedbackDebounce
{
	public interface IFeedbackDebounce<T>
	{
		event EventHandler<GenericEventArgs<T>> OnValue;
		void Enqueue(T item);
	}
}
