using System;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;

namespace ICD.Connect.Protocol.Network.Ports.Web.WebQueues
{
	public sealed class WebQueue : IDisposable
	{
		#region Private Members

		private readonly PriorityQueue<IcdWebRequest> m_RequestQueue;
		private readonly SafeCriticalSection m_RequestSection;

		private readonly SafeTimer m_RequestDelayTimer;
		private bool m_RequestDelayRunning;

		private IcdWebRequest m_CurrentRequest;
		private bool m_RequestIsRunning;

		private readonly SafeTimer m_DisconnectClearTimer;

		#endregion

		#region Properties

		public IWebPort Port { get; private set; }

		public long RequestDelayTime { get; set; }

		#endregion

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		public WebQueue()
		{
			m_RequestDelayTimer = SafeTimer.Stopped(RequestDelayTimerElapsed);
			m_RequestQueue = new PriorityQueue<IcdWebRequest>();
			m_RequestSection = new SafeCriticalSection();
			m_DisconnectClearTimer = SafeTimer.Stopped(DisconnectedClearCallback);
		}

		public void Dispose()
		{
			m_RequestDelayTimer.Stop();
			m_DisconnectClearTimer.Stop();

			SetPort(null);
		}

		#endregion

		#region Methods

		public void SetPort(IWebPort port)
		{
			Port = port;
		}

		public void Clear()
		{
			m_RequestSection.Enter();

			try
			{
				m_RequestQueue.Clear();
				m_CurrentRequest = null;
			}
			finally
			{
				m_RequestSection.Leave();
			}
		}

		public void Enqueue(IcdWebRequest request)
		{
			if (request == null)
				throw new ArgumentNullException("request");

			EnqueuePriority(request, int.MaxValue);
		}

		public void Enqueue(IcdWebRequest request, Func<IcdWebRequest, IcdWebRequest, bool> comparer)
		{
			if (request == null)
				throw new ArgumentNullException("request");

			EnqueuePriority(request, comparer, int.MaxValue, false);
		} 

		public void EnqueuePriority(IcdWebRequest request)
		{
			if (request == null)
				throw new ArgumentNullException("request");

			EnqueuePriority(request, 0);
		}

		public void EnqueuePriority(IcdWebRequest request, int priority)
		{
			if (request == null)
				throw new ArgumentNullException("request");

			EnqueuePriority(request, (a, b) => false, priority);
		}

		public void EnqueuePriority(IcdWebRequest request, Func<IcdWebRequest, IcdWebRequest, bool> comparer, int priority)
		{
			if (request == null)
				throw new ArgumentNullException("request");

			EnqueuePriority(request, comparer, priority, false);
		}

		private void EnqueuePriority(IcdWebRequest request, Func<IcdWebRequest, IcdWebRequest, bool> comparer, int priority, bool deDuplicateToEndofQueue)
		{
			if (request == null)
				throw new ArgumentNullException("request");

			m_RequestSection.Execute(() =>
			                         m_RequestQueue.EnqueueRemove(request, d => comparer(d, request), priority,
			                                                      deDuplicateToEndofQueue));

			SendNextRequest();
		}

		#endregion

		#region Private Methods

		private void SendNextRequest()
		{
			m_RequestSection.Enter();

			try
			{
				if (m_RequestIsRunning || m_RequestDelayRunning)
					return;

				if (!m_RequestQueue.TryDequeue(out m_CurrentRequest))
					return;

				m_RequestIsRunning = true;
			}
			finally
			{
				m_RequestSection.Leave();
			}

			if (Port == null)
			{
				ServiceProvider.GetService<ILoggerService>()
				               .AddEntry(eSeverity.Error, "{0} failed to send request - Port is null", GetType().Name);
				Clear();
				return;
			}

			try
			{
				WebPortResponse response;
				switch (m_CurrentRequest.RequestType)
				{
					case IcdWebRequest.eWebRequestType.Get:
						response = Port.Get(m_CurrentRequest.RelativeOrAbsoluteUri, m_CurrentRequest.Data);
						break;
					case IcdWebRequest.eWebRequestType.Post:
						response = Port.Post(m_CurrentRequest.RelativeOrAbsoluteUri, m_CurrentRequest.Data);
						break;
					case IcdWebRequest.eWebRequestType.Put:
						response = Port.Put(m_CurrentRequest.RelativeOrAbsoluteUri, m_CurrentRequest.Data);
						break;
					case IcdWebRequest.eWebRequestType.Patch:
						response = Port.Patch(m_CurrentRequest.RelativeOrAbsoluteUri, m_CurrentRequest.Data);
						break;
					case IcdWebRequest.eWebRequestType.Soap:
						response = Port.DispatchSoap(m_CurrentRequest.Action, m_CurrentRequest.Content);
						break;
					default:
						throw new InvalidOperationException();
				}

				ResetRequestDelayTimer();

				if (response.GotResponse)
					m_CurrentRequest.Callback(response);

				FinishRequest();
			}
			catch (ObjectDisposedException)
			{
				Clear();
			}
		}

		private void FinishRequest()
		{
			m_RequestSection.Enter();

			try
			{
				m_CurrentRequest = null;
				m_RequestIsRunning = false;
			}
			finally
			{
				m_RequestSection.Leave();
			}

			SendNextRequest();
		}

		private void ResetRequestDelayTimer()
		{
			if (RequestDelayTime == 0)
				return;

			m_RequestSection.Enter();

			try
			{
				if (!m_RequestDelayRunning)
				{
					m_RequestDelayRunning = true;
					m_RequestDelayTimer.Reset(RequestDelayTime);
				}
			}
			finally
			{
				m_RequestSection.Leave();
			}
		}

		private void RequestDelayTimerElapsed()
		{
			m_RequestSection.Execute(() => m_RequestDelayRunning = false);

			SendNextRequest();
		}

		private void DisconnectedClearCallback()
		{
			Clear();
		}

		#endregion
	}
}