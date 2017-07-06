#if SIMPLSHARP
using Crestron.SimplSharp;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Connect.Protocol.Crosspoints.CrosspointManagers;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using ICD.Connect.Protocol.Sigs;

namespace ICD.Connect.Protocol.Crosspoints.SimplPlus
{
	public sealed class SimplPlusEquipmentCrosspoint
	{
		#region Properties

		private int m_SystemId;

		private int m_CrosspointId;

		private string m_CrosspointName;

		private EquipmentCrosspointManager m_Manager;

		private EquipmentCrosspoint m_Crosspoint;

		private readonly SafeCriticalSection m_SendSection;
		private readonly SafeCriticalSection m_ReceiveSection;

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
		public void InstantiateCrosspoint()
		{
			CrosspointSystem system = SimplPlusStaticCore.Xp3Core.GetOrCreateSystem(m_SystemId);

			m_Manager = system.GetOrCreateEquipmentCrosspointManager();

			m_Crosspoint = new EquipmentCrosspoint(m_CrosspointId, m_CrosspointName);

			m_Manager.RegisterCrosspoint(m_Crosspoint);

			m_Crosspoint.OnSendOutputData += CrosspointOnSendOutputData;

			var statusDelegate = CrosspointStatusCallback;
			if (statusDelegate != null)
				statusDelegate(1);
		}

		[PublicAPI]
		public void DisconnectFromControl(int controlId)
		{
			if (m_Crosspoint != null)
				m_Crosspoint.Deinitialize(controlId);
		}

		[PublicAPI]
		public void Disconnect()
		{
			if (m_Crosspoint != null)
				m_Crosspoint.Deinitialize();
		}

		[PublicAPI]
		public void SendDigital(uint join, ushort value)
		{
			m_SendSection.Enter();

			try
			{
				if (m_Crosspoint != null)
					m_Crosspoint.SendInputSig(join, (value != 0));
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
				if (m_Crosspoint != null)
					m_Crosspoint.SendInputSig(join, value);
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
				if (m_Crosspoint != null)
					m_Crosspoint.SendInputSig(join, value.ToString());
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

		public delegate void DelCrosspointStatus(ushort status);

		[PublicAPI]
		public DelDigitalJoin DigitalSigReceivedCallback { get; set; }

		[PublicAPI]
		public DelAnalogJoin AnalogSigReceivedCallback { get; set; }

		[PublicAPI]
		public DelSerialJoin SerialSigReceivedCallback { get; set; }

		[PublicAPI]
		public DelCrosspointStatus CrosspointStatusCallback { get; set; }

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

		/// <summary>
		/// Default constructor for S+.
		/// </summary>
		[UsedImplicitly]
		public SimplPlusEquipmentCrosspoint()
		{
			m_SendSection = new SafeCriticalSection();
			m_ReceiveSection = new SafeCriticalSection();
		}
	}
}
#endif