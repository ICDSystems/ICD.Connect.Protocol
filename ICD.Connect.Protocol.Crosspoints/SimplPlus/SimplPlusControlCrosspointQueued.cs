#if SIMPLSHARP
using System.Collections.Generic;
using Crestron.SimplSharp;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.Crosspoints.CrosspointManagers;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using ICD.Connect.Protocol.Sigs;

namespace ICD.Connect.Protocol.Crosspoints.SimplPlus
{
	public sealed class SimplPlusControlCrosspointQueued
	{
		#region Properties

		private int m_SystemId;
		private int m_CrosspointId;
		private string m_CrosspointName;

		private ControlCrosspointManager m_Manager;
		private ControlCrosspoint m_Crosspoint;

		private readonly SafeCriticalSection m_SendSection;
		private readonly SafeCriticalSection m_ReceiveSection;

		private readonly Queue<CrosspointData> m_SendQueue;
		private readonly SafeCriticalSection m_SendQueueSection;
		private readonly SafeCriticalSection m_ProcessQueueSection;

		#endregion

		#region SPlusMethods

		[PublicAPI]
		public void SetSystemId(int systemId)
		{
			m_SystemId = systemId;
		}

		[PublicAPI]
		public void SetCrosspointId(int crosspointId)
		{
			m_CrosspointId = crosspointId;
		}

		[PublicAPI]
		public void SetCrosspointName(SimplSharpString name)
		{
			m_CrosspointName = name.ToString();
		}

		[PublicAPI]
		public void InitializeCrosspoint()
		{
			CrosspointSystem system = SimplPlusStaticCore.Xp3Core.GetOrCreateSystem(m_SystemId);

			m_Manager = system.GetOrCreateControlCrosspointManager();

			m_Crosspoint = new ControlCrosspoint(m_CrosspointId, m_CrosspointName);

			m_Manager.RegisterCrosspoint(m_Crosspoint);

			m_Crosspoint.OnSendOutputData += CrosspointOnSendOutputData;
		}

		[PublicAPI]
		public void ConnectToEquipment(int equipmentId)
		{
			m_Crosspoint.Initialize(equipmentId);
		}

		[PublicAPI]
		public void Disconnect()
		{
			m_Crosspoint.Deinitialize();
		}

		[PublicAPI]
		public void SendDigital(uint join, ushort value)
		{
			m_SendSection.Enter();
			try
			{
				var d = new CrosspointData();
				d.AddSig(0, join, value != 0);
				Enqueue(d);
			}
			finally
			{
				m_SendSection.Leave();
			}
		}

		[PublicAPI]
		public void SendAnalog(uint join, ushort value)
		{
			m_SendSection.Enter();
			try
			{
				var d = new CrosspointData();
				d.AddSig(0, join, value);
				Enqueue(d);
			}
			finally
			{
				m_SendSection.Leave();
			}
		}

		[PublicAPI]
		public void SendSerial(uint join, SimplSharpString value)
		{
			m_SendSection.Enter();
			try
			{
				var d = new CrosspointData();
				d.AddSig(0, join, value.ToString());
				Enqueue(d);
			}
			finally
			{
				m_SendSection.Leave();
			}
		}

		#endregion

		#region SPlusDelegates

		public delegate void DelDigitalJoin(uint join, ushort value);

		public delegate void DelAnalogJoin(uint join, ushort value);

		public delegate void DelSerialJoin(uint join, SimplSharpString value);

		[PublicAPI]
		public DelDigitalJoin DigitalSigReceivedCallback { get; set; }

		[PublicAPI]
		public DelAnalogJoin AnalogSigReceivedCallback { get; set; }

		[PublicAPI]
		public DelSerialJoin SerialSigReceivedCallback { get; set; }

		#endregion

		private void CrosspointOnSendOutputData(ICrosspoint sender, CrosspointData data)
		{
			m_ReceiveSection.Enter();

			try
			{
				foreach (Sig sig in data.GetSigs())
				{
					switch (sig.Type)
					{
						case eSigType.Digital:
							DelDigitalJoin callbackD = DigitalSigReceivedCallback;
							if (callbackD != null)
								callbackD(sig.Number, sig.GetBoolValue() ? (ushort)1 : (ushort)0);
							break;
						case eSigType.Analog:
							DelAnalogJoin callbackA = AnalogSigReceivedCallback;
							if (callbackA != null)
								callbackA(sig.Number, sig.GetUShortValue());
							break;
						case eSigType.Serial:
							DelSerialJoin callbackS = SerialSigReceivedCallback;
							if (callbackS != null)
								callbackS(sig.Number, new SimplSharpString(sig.GetStringValue() ?? ""));
							break;
					}
				}
			}
			finally
			{
				m_ReceiveSection.Leave();
			}
		}

		private void Enqueue(CrosspointData data)
		{
			m_SendQueueSection.Execute(() => m_SendQueue.Enqueue(data));
			ProcessSendQueue();
		}

		private void ProcessSendQueue()
		{
			if (!m_ProcessQueueSection.TryEnter())
				return;

			try
			{
				CrosspointData data = null;

				while (m_SendQueueSection.Execute(() => m_SendQueue.Dequeue(out data)))
				{
					if (data != null)
						m_Crosspoint.SendInputData(data);
				}
			}
			finally
			{
				m_ProcessQueueSection.Leave();
			}
		}

		/// <summary>
		/// Default constructor for S+.
		/// </summary>
		[UsedImplicitly]
		public SimplPlusControlCrosspointQueued()
		{
			m_SendSection = new SafeCriticalSection();
			m_ReceiveSection = new SafeCriticalSection();

			m_SendQueue = new Queue<CrosspointData>(20);
			m_SendQueueSection = new SafeCriticalSection();
			m_ProcessQueueSection = new SafeCriticalSection();
		}
	}
}
#endif