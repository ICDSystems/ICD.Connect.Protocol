using System;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.Crosspoints.CrosspointManagers;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using ICD.Connect.Protocol.Crosspoints.EventArguments;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.Sigs;
using ICD.Connect.Protocol.XSig;
using ICD.Connect.Settings.SPlusShims;

namespace ICD.Connect.Protocol.Crosspoints.SimplPlus.CrosspointShims
{
	public abstract class AbstractSimplPlusXSigCrosspointShim<T> : AbstractSPlusShim, ISimplPlusCrosspointShim<T>
		where T : class, ICrosspoint
	{
		#region Events

		// Events for S+
		[PublicAPI("S+")]
		public event EventHandler<IntEventArgs> OnSystemIdChanged;

		[PublicAPI("S+")]
		public event EventHandler<IntEventArgs> OnCrosspointIdChanged;

		[PublicAPI("S+")]
		public event EventHandler<StringEventArgs> OnCrosspointNameChanged;

		#endregion

		#region Callbacks

		[PublicAPI("S+")]
		public SPlusJoinXSigCallback DigitalSigReceivedXSigCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusJoinXSigCallback AnalogSigReceivedXSigCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusJoinXSigCallback SerialSigReceivedXSigCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusStatusUpdateCallback CrosspointStatusCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusCrosspointChangedCallback CrosspointChangedCallback { get; set; }

		#endregion

		#region Private Members

		private readonly XSigSerialBuffer m_Buffer;
		private readonly SafeCriticalSection m_SendSection;
		private readonly SafeCriticalSection m_ReceiveSection;

		private int m_SystemId;
		private int m_CrosspointId;
		private string m_CrosspointName;

		#endregion

		#region Public Properties

		[PublicAPI("S+")]
		public int SystemId
		{
			get { return m_SystemId; }
			private set
			{
				if (m_SystemId == value)
					return;

				m_SystemId = value;

				OnSystemIdChanged.Raise(this, new IntEventArgs(m_SystemId));
			}
		}

		[PublicAPI("S+")]
		public int CrosspointId
		{
			get { return m_CrosspointId; }
			private set
			{
				if(m_CrosspointId == value)
					return;

				m_CrosspointId = value;

				OnCrosspointIdChanged.Raise(this, new IntEventArgs(m_CrosspointId));
			}
		}

		[PublicAPI("S+")]
		public string CrosspointName
		{
			get { return m_CrosspointName; }
			private set
			{
				if (m_CrosspointName == value)
					return;

				m_CrosspointName = value;

				OnCrosspointNameChanged.Raise(this, new StringEventArgs(m_CrosspointName));
			}
		}

		[CanBeNull]
		ICrosspoint ISimplPlusCrosspointShim.Crosspoint { get { return Crosspoint; } }

		[CanBeNull]
		public T Crosspoint { get; private set; }

		[CanBeNull]
		protected abstract ICrosspointManager Manager { get; }

		[CanBeNull]
		protected CrosspointSystem System
		{
			get { return SystemId == 0 ? null : SimplPlusStaticCore.Xp3Core.GetOrCreateSystem(SystemId); }
		}

		#endregion

		/// <summary>
		/// Default constructor for S+.
		/// </summary>
		[PublicAPI("S+")]
		protected AbstractSimplPlusXSigCrosspointShim()
		{
			m_SendSection = new SafeCriticalSection();
			m_ReceiveSection = new SafeCriticalSection();

			m_Buffer = new XSigSerialBuffer();
			m_Buffer.OnCompletedSerial += BufferOnCompletedSerial;

			SimplPlusStaticCore.ShimManager.RegisterSPlusCrosspointShim(this);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public override void Dispose()
		{
			OnSystemIdChanged = null;
			OnCrosspointIdChanged = null;
			OnCrosspointNameChanged = null;

			base.Dispose();
		}

		#region Public S+ Methods

		[PublicAPI("S+")]
		public void SetSystemId(int systemId)
		{
			SystemId = systemId;
		}

		[PublicAPI("S+")]
		public void SetCrosspointId(int crosspointId)
		{
			CrosspointId = crosspointId;
		}

		[PublicAPI("S+")]
		public void SetCrosspointName(string name)
		{
			CrosspointName = name;
		}

		/// <summary>
		/// Instantiate the crosspoint, registers it with the manager, and readies it for use.
		/// </summary>
		[PublicAPI("S+")]
		public void InstantiateCrosspoint()
		{
			// Deinstantiate existing crosspoint, if it exists
			if (Crosspoint != null)
				DeinstantiateCrosspoint();

			// Bail out if all the information isn't set from S+ already
			if (Manager == null || CrosspointId == 0 || string.IsNullOrEmpty(CrosspointName))
				return;

			Crosspoint = CreateCrosspoint(CrosspointId, CrosspointName);

			Manager.RegisterCrosspoint(Crosspoint);

			RegisterCrosspoint();
		}

		/// <summary>
		/// Unregisteres the crosspoint from the manager, and deinstantiates it.
		/// </summary>
		[PublicAPI("S+")]
		public void DeinstantiateCrosspoint()
		{
			// Don't do things without a crosspoint
			if (Manager == null || Crosspoint == null)
				return;

			Manager.UnregisterCrosspoint(Crosspoint);

			UnregisterCrosspoint();

			Crosspoint.Dispose();

			Crosspoint = default(T);
		}

		[PublicAPI("S+")]
		public abstract void Disconnect();

		[PublicAPI("S+")]
		public void SendXSig(string xsig)
		{
			//Don't do things without a crosspoint
			if (Crosspoint == null)
				return;

			m_SendSection.Enter();

			try
			{
				m_Buffer.Enqueue(xsig);
			}
			finally
			{
				m_SendSection.Leave();
			}
		}

		#endregion

		#region Public S# Methods

		public string GetShimInfo()
		{
			TableBuilder tb = new TableBuilder("Property", "Value");

			tb.AddRow("Shim Type", GetType());
			tb.AddRow("Shim Location", Location);
			tb.AddRow("Shim Has Crosspoint", Crosspoint == null ? "False" : "True");

			if (Manager == null || Crosspoint == null)
			{
				tb.AddRow("Shim SystemID", SystemId);
				tb.AddRow("Shim CrosspointId", CrosspointId);
				tb.AddRow("Shim Crosspoint Name", CrosspointName ?? "Undefined");
			}
			else
			{
				tb.AddRow("Crosspoint SystemID", Manager.SystemId);
				tb.AddRow("Crosspoint Id", Crosspoint.Id);
				tb.AddRow("Crosspoint Name", Crosspoint.Name ?? "Undefined");
				tb.AddRow("Crosspoint Status", Crosspoint.Status.ToString());
			}

			return tb.ToString();
		}

		#endregion

		#region Private / Protected Methods

		protected abstract void RegisterCrosspoint();
		protected abstract void UnregisterCrosspoint();
		protected abstract T CreateCrosspoint(int id, string name);

		protected void CrosspointOnSendOutputData(ICrosspoint sender, CrosspointData data)
		{
			m_ReceiveSection.Enter();

			try
			{
				if (data.MessageType == CrosspointData.eMessageType.Ping || data.MessageType == CrosspointData.eMessageType.Pong)
					return;

				//Only pass along SmartObject of 0
				//todo: Setup for other modules to hook in for smart object joins
				foreach (SigInfo sig in data.GetSigs().Where(sig => sig.SmartObject == 0))
				{
					SPlusJoinXSigCallback handler;
					switch (sig.Type)
					{
						case eSigType.Digital:
							handler = DigitalSigReceivedXSigCallback;
							break;

						case eSigType.Analog:
							handler = AnalogSigReceivedXSigCallback;
							break;

						case eSigType.Serial:
							handler = SerialSigReceivedXSigCallback;
							break;

						default:
							throw new ArgumentOutOfRangeException();
					}

					if (handler != null)
						handler(sig.ToXSig());
				}
			}
			finally
			{
				m_ReceiveSection.Leave();
			}
		}

		protected void CrosspointOnStatusChanged(object sender, CrosspointStatusEventArgs args)
		{
			SPlusStatusUpdateCallback callback = CrosspointStatusCallback;
			if (callback != null)
				callback((ushort)args.Data);
		}

		private void BufferOnCompletedSerial(object sender, StringEventArgs stringEventArgs)
		{
			if (Crosspoint == null)
				return;

			SigInfo sig = XSigParser.Parse(stringEventArgs.Data).ToSigInfo();
			CrosspointData data = new CrosspointData();
			data.AddSig(sig);

			Crosspoint.SendInputData(data);
		}

		#endregion
	}
}
