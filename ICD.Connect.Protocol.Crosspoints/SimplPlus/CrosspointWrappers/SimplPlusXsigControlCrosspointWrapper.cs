using System.Linq;
#if SIMPLSHARP
using System;
using System.Collections.Generic;
using Crestron.SimplSharp;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Connect.Protocol.Crosspoints.CrosspointManagers;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using ICD.Connect.Protocol.Crosspoints.EventArguments;
using ICD.Connect.Protocol.Sigs;
using ICD.Connect.Protocol.XSig;

namespace ICD.Connect.Protocol.Crosspoints.SimplPlus.CrosspointWrappers
{
	public sealed class SimplPlusXSigControlCrosspointWrapper : ISimplPlusCrosspointWrapper
	{
		#region Fields

		private int m_SystemId;

		private int m_CrosspointId;

		private string m_CrosspointName;

		private string m_CrosspointSymbolInstanceName;

		private ControlCrosspointManager m_Manager;

		private ControlCrosspoint m_Crosspoint;

		private readonly SafeCriticalSection m_SendSection;
		private readonly SafeCriticalSection m_ReceiveSection;

		#endregion

		#region Events

		public event EventHandler OnCrosspointChanged;

		#endregion

		#region Properties

		public int SystemId { get { return m_SystemId; } }

		public int CrosspointId { get { return m_CrosspointId; } }

		public string CrosspointName { get { return m_CrosspointName; } }

		public string CrosspointSymbolInstanceName { get { return m_CrosspointSymbolInstanceName; } }

		public ICrosspoint Crosspoint { get { return m_Crosspoint; } }

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
		public void SetCrosspointName(string name)
		{
			m_CrosspointName = name;
		}

		[PublicAPI]
		public void SetCrosspointSymbolInstanceName(string name)
		{
			m_CrosspointSymbolInstanceName = name;
		}

		/// <summary>
		/// Instantiate the crosspoint, registers it with the manager, and readies it for use.
		/// </summary>
		[PublicAPI]
		public void InstantiateCrosspoint()
		{
			// Deinstantiate existing crosspoint, if it exists
			if (m_Crosspoint != null)
				DeinstantiateCrosspoint();

			// Bail out if all the information isn't set from S+ already
			if (m_SystemId == 0 || m_CrosspointId == 0 || String.IsNullOrEmpty(m_CrosspointName))
				return;

			CrosspointSystem system = SimplPlusStaticCore.Xp3Core.GetOrCreateSystem(m_SystemId);

			m_Manager = system.GetOrCreateControlCrosspointManager();

			m_Crosspoint = new ControlCrosspoint(m_CrosspointId, m_CrosspointName);

			m_Manager.RegisterCrosspoint(m_Crosspoint);

			RegisterCrosspoint();
		}

		/// <summary>
		/// Unregisteres the crosspoint from the manager, and deinstantiates it.
		/// </summary>
		[PublicAPI]
		public void DeinstantiateCrosspoint()
		{
			//Don't do things without a crosspoint
			if (m_Crosspoint == null)
				return;

			m_Manager.UnregisterCrosspoint(m_Crosspoint);

			UnregisterCrosspoint();

			m_Crosspoint.Dispose();

			m_Crosspoint = null;
		}

		[PublicAPI]
		public void ConnectToEquipment(int equipmentId)
		{
			//Don't do things without a crosspoint
			if (m_Crosspoint == null)
				return;

			m_Crosspoint.Initialize(equipmentId);
		}

		[PublicAPI]
		public void Disconnect()
		{
			//Don't do things without a crosspoint
			if (m_Crosspoint == null)
				return;

			m_Crosspoint.Deinitialize();
		}

		[PublicAPI]
		public void SendXSig(SimplSharpString xsig)
		{
			//Don't do things without a crosspoint
			if (m_Crosspoint == null)
				return;

			m_SendSection.Enter();

			try
			{
				IEnumerable<SigInfo> sigs = XSigParser.ParseMultiple(xsig.ToString()).Select(s => s.ToSigInfo());
				CrosspointData data = new CrosspointData();
				data.AddSigs(sigs);
				m_Crosspoint.SendInputData(data);
			}
			finally
			{
				m_SendSection.Leave();
			}
		}

		/// <summary>
		/// When true, the crosspoint manager will attempt to reconnect when a connection is dropped.
		/// </summary>
		/// <param name="autoReconnect"></param>
		[PublicAPI]
		public void SetAutoReconnect(ushort autoReconnect)
		{
			if (m_Manager == null)
				return;

			m_Manager.AutoReconnect = autoReconnect != 0;
		}

		#endregion

		#region SPlusDelegates

		public delegate void DelJoinXSig(SimplSharpString xsig);

		public delegate void DelStatusUpdate(ushort status);

		[PublicAPI]
		public DelJoinXSig DigitalSigReceivedXSigCallback { get; set; }

		[PublicAPI]
		public DelJoinXSig AnalogSigReceivedXSigCallback { get; set; }

		[PublicAPI]
		public DelJoinXSig SerialSigReceivedXSigCallback { get; set; }

		[PublicAPI]
		public DelStatusUpdate CrosspointStatusCallback { get; set; }

		#endregion

		public string GetWrapperInfo()
		{
			TableBuilder tb = new TableBuilder("Property", "Value");

			tb.AddRow("Wrapper Type", GetType());
			tb.AddRow("Wrapper Location", CrosspointSymbolInstanceName);
			tb.AddRow("Wrapper Has Crosspoint", m_Crosspoint == null ? "False" : "True");

			if (m_Crosspoint == null)
			{
				tb.AddRow("Wrapper SystemID", m_SystemId);
				tb.AddRow("Wrapper CrosspointId", m_CrosspointId);
				tb.AddRow("Wrapper Crosspoint Name", m_CrosspointName ?? "Undefined");
			}
			else
			{
				tb.AddRow("Crosspoint SystemID", m_Manager.SystemId);
				tb.AddRow("Crosspoint Id", m_CrosspointId);
				tb.AddRow("Crosspoint Name", m_CrosspointName ?? "Undefined");
				tb.AddRow("Crosspoint Status", m_Crosspoint.Status.ToString());
			}

			return tb.ToString();
		}

		private void RegisterCrosspoint()
		{
			m_Crosspoint.OnSendOutputData += CrosspointOnSendOutputData;
			m_Crosspoint.OnStatusChanged += CrosspointOnStatusChanged;

			DelStatusUpdate statusCallback = CrosspointStatusCallback;
			if (statusCallback != null)
				statusCallback((ushort)m_Crosspoint.Status);

			EventHandler crosspointChangedEvent = OnCrosspointChanged;
			if (crosspointChangedEvent != null)
				crosspointChangedEvent(this, EventArgs.Empty);
		}

		private void UnregisterCrosspoint()
		{
			m_Crosspoint.OnSendOutputData -= CrosspointOnSendOutputData;
			m_Crosspoint.OnStatusChanged -= CrosspointOnStatusChanged;

			DelStatusUpdate statusCallback = CrosspointStatusCallback;
			if (statusCallback != null)
				statusCallback(0);

			EventHandler crosspointChangedEvent = OnCrosspointChanged;
			if (crosspointChangedEvent != null)
				crosspointChangedEvent(this, EventArgs.Empty);
		}

		private void CrosspointOnSendOutputData(ICrosspoint sender, CrosspointData data)
		{
			m_ReceiveSection.Enter();

			try
			{
				if (data.MessageType == CrosspointData.eMessageType.Ping || data.MessageType == CrosspointData.eMessageType.Pong)
					return;

				foreach (SigInfo sig in data.GetSigs())
				{
					//Only pass along SmartObject of 0
					//todo: Setup for other modules to hook in for smart object joins
					if (sig.SmartObject != 0)
						continue;

					DelJoinXSig callback;

					switch (sig.Type)
					{
						case eSigType.Digital:
							callback = DigitalSigReceivedXSigCallback;
							break;

						case eSigType.Analog:
							callback = AnalogSigReceivedXSigCallback;
							break;

						case eSigType.Serial:
							callback = SerialSigReceivedXSigCallback;
							break;

						default:
							throw new ArgumentOutOfRangeException();
					}

					if (callback != null)
						callback(sig.ToXSig());
				}
			}
			finally
			{
				m_ReceiveSection.Leave();
			}
		}

		private void CrosspointOnStatusChanged(object sender, CrosspointStatusEventArgs args)
		{
			DelStatusUpdate callback = CrosspointStatusCallback;
			if (callback != null)
				callback((ushort)args.Data);
		}

		/// <summary>
		/// Default constructor for S+.
		/// </summary>
		[UsedImplicitly]
		public SimplPlusXSigControlCrosspointWrapper()
		{
			m_SendSection = new SafeCriticalSection();
			m_ReceiveSection = new SafeCriticalSection();
			m_CrosspointSymbolInstanceName = "";
			SimplPlusStaticCore.WrapperManager.RegisterSPlusCrosspointWrapper(this);
		}
	}
}

#endif
