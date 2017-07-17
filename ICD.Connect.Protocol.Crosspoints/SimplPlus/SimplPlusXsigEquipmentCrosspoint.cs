using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.Crosspoints.CrosspointManagers;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using ICD.Connect.Protocol.Sigs;
using ICD.Connect.Protocol.XSig;

namespace ICD.Connect.Protocol.Crosspoints.SimplPlus
{
	public class SimplPlusXsigEquipmentCrosspoint
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
		public void SendXsig(SimplSharpString xsig)
		{
			m_SendSection.Enter();

			try
			{
				if (m_Crosspoint != null)
				{
					IEnumerable<Sig> sigs = Xsig.ParseMultiple(xsig.ToString()); 
					CrosspointData data = new CrosspointData();
					data.AddSigs(sigs);
					m_Crosspoint.SendInputData(data);
				}
			}
			finally
			{
				m_SendSection.Leave();
			}
		}

		#endregion

		#region SPlusDelegates

		public delegate void DelDigitalJoinXsig(SimplSharpString xsig);

		public delegate void DelAnalogJoinXsig(SimplSharpString xsig);

		public delegate void DelSerialJoinXsig(SimplSharpString xsig);

		public delegate void DelCrosspointStatus(ushort status);

		[PublicAPI]
		public DelDigitalJoinXsig DigitalSigReceivedXsigCallback { get; set; }

		[PublicAPI]
		public DelAnalogJoinXsig AnalogSigReceivedXsigCallback { get; set; }

		[PublicAPI]
		public DelSerialJoinXsig SerialSigReceivedXsigCallback { get; set; }

		[PublicAPI]
		public DelCrosspointStatus CrosspointStatusCallback { get; set; }

		#endregion

		private void CrosspointOnSendOutputData(ICrosspoint sender, CrosspointData data)
		{
			m_ReceiveSection.Enter();

			try
			{
				StringBuilder dBuilder = new StringBuilder();
				StringBuilder aBuilder = new StringBuilder();
				foreach (Sig sig in data.GetSigs())
				{
					switch (sig.Type)
					{
						case eSigType.Digital:
							var d = new DigitalXsig(sig.GetBoolValue(), (ushort)(sig.Number-1));
							dBuilder.Append(StringUtils.ToString(d.Data));
							break;
						case eSigType.Analog:
							var a = new AnalogXsig(sig.GetUShortValue(), (ushort)(sig.Number - 1));
							aBuilder.Append(StringUtils.ToString(a.Data));
							break;
						case eSigType.Serial:
							var s = new SerialXsig(sig.GetStringValue(), (ushort)(sig.Number - 1));
							DelSerialJoinXsig callback = SerialSigReceivedXsigCallback;
							if (callback != null)
								callback(StringUtils.ToString(s.Data));
							break;
					}
				}
				foreach (var xsig in dBuilder.ToString().Split(254))
				{
					DelDigitalJoinXsig callback = DigitalSigReceivedXsigCallback;
					if (callback != null)
						callback(xsig);
				}
				foreach (var xsig in aBuilder.ToString().Split(252))
				{
					DelAnalogJoinXsig callback = AnalogSigReceivedXsigCallback;
					if (callback != null)
						callback(xsig);
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
		public SimplPlusXsigEquipmentCrosspoint()
		{
			m_SendSection = new SafeCriticalSection();
			m_ReceiveSection = new SafeCriticalSection();
		}
	}
}