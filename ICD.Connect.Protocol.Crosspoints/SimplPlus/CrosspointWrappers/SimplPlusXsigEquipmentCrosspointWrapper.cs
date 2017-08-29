#if SIMPLSHARP
using System;
using System.Collections.Generic;
using System.Text;
using Crestron.SimplSharp;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.Crosspoints.CrosspointManagers;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using ICD.Connect.Protocol.Crosspoints.EventArguments;
using ICD.Connect.Protocol.Sigs;
using ICD.Connect.Protocol.XSig;

namespace ICD.Connect.Protocol.Crosspoints.SimplPlus.CrosspointWrappers
{
	public class SimplPlusXsigEquipmentCrosspointWrapper : ISimplPlusCrosspointWrapper
	{
		#region Fields

		private int m_SystemId;

		private int m_CrosspointId;

		private string m_CrosspointName;

		private string m_CrosspointSymbolInstanceName;

		private EquipmentCrosspointManager m_Manager;

		private EquipmentCrosspoint m_Crosspoint;

		private readonly SafeCriticalSection m_SendSection;
		private readonly SafeCriticalSection m_ReceiveSection;
		
		#endregion


		#region Properties

		public int SystemId { get { return m_SystemId; } }

		public int CrosspointId { get { return m_CrosspointId; } }

		public string CrosspointName { get { return m_CrosspointName; } }

		public string CrosspointSymbolInstanceName { get { return m_CrosspointSymbolInstanceName; } }

		public ICrosspoint Crosspoint { get { return m_Crosspoint; } }

		#endregion

		#region Events

		public event EventHandler OnCrosspointChanged;

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

			m_Manager = system.GetOrCreateEquipmentCrosspointManager();

			m_Crosspoint = new EquipmentCrosspoint(m_CrosspointId, m_CrosspointName);

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
		public void DisconnectFromControl(int controlId)
		{
			//Don't do things without a crosspoint
			if (m_Crosspoint == null)
				return;

			m_Crosspoint.Deinitialize(controlId);
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
		public void SendXsig(SimplSharpString xsig)
		{
			//Don't do things without a crosspoint
			if (m_Crosspoint == null)
				return;

			m_SendSection.Enter();

			try
			{
				IEnumerable<Sig> sigs = Xsig.ParseMultiple(xsig.ToString());
				CrosspointData data = new CrosspointData();
				data.AddSigs(sigs);
				m_Crosspoint.SendInputData(data);
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

		public delegate void DelStatusUpdate(ushort status);

		[PublicAPI]
		public DelDigitalJoinXsig DigitalSigReceivedXsigCallback { get; set; }

		[PublicAPI]
		public DelAnalogJoinXsig AnalogSigReceivedXsigCallback { get; set; }

		[PublicAPI]
		public DelSerialJoinXsig SerialSigReceivedXsigCallback { get; set; }

		[PublicAPI]
		public DelStatusUpdate CrosspointStatusCallback { get; set; }

		[PublicAPI]
		public DelStatusUpdate ControlCrosspointsConnectedCallback { get; set; }

		#endregion

		public string GetWrapperInfo()
		{
			StringBuilder sb = new StringBuilder();

			TableBuilder tb = new TableBuilder("Property", "Value");

			tb.AddRow("Wrapper Type", this.GetType());
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
				tb.AddRow("Crosspoint Connections", m_Crosspoint.ControlCrosspointsCount);
			}

			return tb.ToString();
		}

		private void RegisterCrosspoint()
		{
			m_Crosspoint.OnSendOutputData += CrosspointOnSendOutputData;
			m_Crosspoint.OnStatusChanged += CrosspointOnStatusChanged;
			m_Crosspoint.OnControlCrosspointCountChanged += CrosspointOnControlCrosspointCountChanged;

			var statusCallback = CrosspointStatusCallback;
			if (statusCallback != null)
				statusCallback((ushort)m_Crosspoint.Status);

			var connectionsCallback = ControlCrosspointsConnectedCallback;
			if (connectionsCallback != null)
				connectionsCallback((ushort)m_Crosspoint.ControlCrosspointsCount);

			var crosspointChangedEvent = OnCrosspointChanged;
			if (crosspointChangedEvent != null)
				crosspointChangedEvent(this, EventArgs.Empty);
		}

		private void UnregisterCrosspoint()
		{
			m_Crosspoint.OnSendOutputData -= CrosspointOnSendOutputData;
			m_Crosspoint.OnStatusChanged -= CrosspointOnStatusChanged;
			m_Crosspoint.OnControlCrosspointCountChanged -= CrosspointOnControlCrosspointCountChanged;

			var statusCallback = CrosspointStatusCallback;
			if (statusCallback != null)
				statusCallback((ushort)0);

			var connectedControlsCallback = ControlCrosspointsConnectedCallback;
			if (connectedControlsCallback != null)
				connectedControlsCallback((ushort)0);

			var crosspointChangedEvent = OnCrosspointChanged;
			if (crosspointChangedEvent != null)
				crosspointChangedEvent(this, EventArgs.Empty);
		}

		private void CrosspointOnSendOutputData(ICrosspoint sender, CrosspointData data)
		{
			m_ReceiveSection.Enter();

			try
			{
				StringBuilder dBuilder = new StringBuilder();
				StringBuilder aBuilder = new StringBuilder();
				foreach (Sig sig in data.GetSigs())
				{
					//Only pass along SmartObject of 0
					//todo: Setup for other modules to hook in for smart object joins
					if (sig.SmartObject != 0)
						continue;

					switch (sig.Type)
					{
						case eSigType.Digital:
							var d = new DigitalXsig(sig.GetBoolValue(), (ushort)(sig.Number - 1));
							dBuilder.Append(StringUtils.ToString(d.Data));
							break;
						case eSigType.Analog:
							var a = new AnalogXsig(sig.GetUShortValue(), (ushort)(sig.Number - 1));
							aBuilder.Append(StringUtils.ToString(a.Data));
							break;
						case eSigType.Serial:
							var s = new SerialXsig(sig.GetStringValue() ?? "", (ushort)(sig.Number - 1));
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

		private void CrosspointOnStatusChanged(object sender, CrosspointStatusEventArgs args)
		{
			DelStatusUpdate callback = CrosspointStatusCallback;
			if (callback != null)
				callback((ushort)args.Data);
		}

		private void CrosspointOnControlCrosspointCountChanged(object sender, IntEventArgs args)
		{
			DelStatusUpdate callback = ControlCrosspointsConnectedCallback;
			if (callback != null)
				callback((ushort)args.Data);
		}

		/// <summary>
		/// Default constructor for S+.
		/// </summary>
		[UsedImplicitly]
		public SimplPlusXsigEquipmentCrosspointWrapper()
		{
			m_SendSection = new SafeCriticalSection();
			m_ReceiveSection = new SafeCriticalSection();
			m_CrosspointSymbolInstanceName = "";
			SimplPlusStaticCore.WrapperManager.RegisterSPlusCrosspointWrapper(this);
		}
	}
}
#endif