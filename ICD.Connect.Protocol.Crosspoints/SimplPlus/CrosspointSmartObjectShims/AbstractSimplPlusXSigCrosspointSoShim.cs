using System;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.Crosspoints.CrosspointManagers;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using ICD.Connect.Protocol.Crosspoints.EventArguments;
using ICD.Connect.Protocol.Crosspoints.SimplPlus.CrosspointShims;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.Sigs;
using ICD.Connect.Protocol.XSig;

namespace ICD.Connect.Protocol.Crosspoints.SimplPlus.CrosspointSmartObjectShims
{
	public abstract class AbstractSimplPlusXSigCrosspointSoShim
	{

		#region Events

		// Events for S+
		[PublicAPI("S+")]
		public event EventHandler<IntEventArgs> OnSystemIdChanged;

		[PublicAPI("S+")]
		public event EventHandler<IntEventArgs> OnCrosspointIdChanged;

		[PublicAPI("S+")]
		public event EventHandler<IntEventArgs> OnSmartObjectIdChanged;



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
		private ushort m_SmartObjectId;
		private IControlCrosspoint m_Crosspoint;

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

				Unsubscribe(System);

				m_SystemId = value;

				Subscribe(System);

				OnSystemIdChanged.Raise(this, new IntEventArgs(m_SystemId));
			}
		}

		[PublicAPI("S+")]
		public int CrosspointId
		{
			get { return m_CrosspointId; }
			private set
			{
				if (m_CrosspointId == value)
					return;

				m_CrosspointId = value;

				Crosspoint = GetCrosspoint(CrosspointId);

				OnCrosspointIdChanged.Raise(this, new IntEventArgs(m_CrosspointId));
			}
		}

		[PublicAPI("S+")]
		public ushort SmartObjectId
		{
			get { return m_SmartObjectId; }
			private set
			{
				if (m_SmartObjectId == value)
					return;

				m_SmartObjectId = value;

				OnSmartObjectIdChanged.Raise(this, new IntEventArgs(m_SmartObjectId));
			}
		}

		[CanBeNull]
		public IControlCrosspoint Crosspoint
		{
			get { return m_Crosspoint; }
			private set
			{
				if (m_Crosspoint == value)
					return;

				Unsubscribe(m_Crosspoint);

				m_Crosspoint = value;

				Subscribe(m_Crosspoint);
				
				UpdateCrosspointStatus();

				InvokeCrosspointChangedCallback();
			}
		}

		[CanBeNull]
		private ControlCrosspointManager Manager
		{
			get { return System == null ? null : System.GetOrCreateControlCrosspointManager(); }
		}

		[CanBeNull]
		private CrosspointSystem System
		{
			get { return SystemId == 0 ? null : SimplPlusStaticCore.Xp3Core.GetOrCreateSystem(SystemId); }
		}

		#endregion

		#region Protected Properties

		protected uint DigitalJoinOffset { get { return 0; } }

		protected uint AnalogJoinOffset { get { return 0; }}

		protected uint SerialJoinOffset { get { return 0; } }

		#endregion

		/// <summary>
		/// Default constructor for S+.
		/// </summary>
		[PublicAPI("S+")]
		protected AbstractSimplPlusXSigCrosspointSoShim()
		{
			m_SendSection = new SafeCriticalSection();
			m_ReceiveSection = new SafeCriticalSection();

			m_Buffer = new XSigSerialBuffer();
			m_Buffer.OnCompletedSerial += BufferOnCompletedSerial;
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			OnSystemIdChanged = null;
			OnCrosspointIdChanged = null;
			OnSmartObjectIdChanged = null;
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
		public void SetSmartObjectId(ushort smartObjectId)
		{
			SmartObjectId = smartObjectId;
		}

		[PublicAPI("S+")]
		public void SendXSig(string xsig)
		{
			//Don't do things without a crosspoint or SmartObjectId
			if (Crosspoint == null || SmartObjectId == 0)
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

		#region Crosspoint System Events

		private void Subscribe(CrosspointSystem system)
		{
			if (system == null)
				return;

			system.GetOrCreateControlCrosspointManager().OnCrosspointRegistered += ManagerOnCrosspointRegistered;
			system.GetOrCreateControlCrosspointManager().OnCrosspointUnregistered += ManagerOnCrosspointUnregistered;

			//try to get new crosspoint
			Crosspoint = GetCrosspoint(CrosspointId);
		}

		private void Unsubscribe(CrosspointSystem system)
		{
			if (system == null)
				return;

			system.GetOrCreateControlCrosspointManager().OnCrosspointRegistered -= ManagerOnCrosspointRegistered;
			system.GetOrCreateControlCrosspointManager().OnCrosspointUnregistered -= ManagerOnCrosspointUnregistered;
		}

		private void ManagerOnCrosspointRegistered(ICrosspointManager sender, ICrosspoint crosspoint)
		{
			IControlCrosspoint controlCrosspoint = crosspoint as IControlCrosspoint;
			if (CrosspointId == 0 || controlCrosspoint == null || controlCrosspoint.Id != CrosspointId )
				return;

			Crosspoint = controlCrosspoint;
		}

		private void ManagerOnCrosspointUnregistered(ICrosspointManager sender, ICrosspoint crosspoint)
		{
			IControlCrosspoint controlCrosspoint = crosspoint as IControlCrosspoint;
			if (CrosspointId == 0 || controlCrosspoint == null || controlCrosspoint.Id != CrosspointId)
				return;

			Crosspoint = null;
		}

		#endregion

		#region Crosspoint Events

		/// <summary>
		/// Subscribe from the crosspoint
		/// </summary>
		/// <param name="crosspoint"></param>
		private void Subscribe(IControlCrosspoint crosspoint)
		{
			if (crosspoint == null)
				return;

			crosspoint.OnSendOutputData += CrosspointOnSendOutputData;
			crosspoint.OnStatusChanged += CrosspointOnStatusChanged;
		}

		/// <summary>
		/// Unsubscribe to the crosspoint
		/// </summary>
		/// <param name="crosspoint"></param>
		private void Unsubscribe(IControlCrosspoint crosspoint)
		{
			if (crosspoint == null)
				return;

			crosspoint.OnSendOutputData -= CrosspointOnSendOutputData;
			crosspoint.OnStatusChanged -= CrosspointOnStatusChanged;
		}

		/// <summary>
		/// Handle crosspoint status changes
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void CrosspointOnStatusChanged(object sender, CrosspointStatusEventArgs args)
		{
			SPlusStatusUpdateCallback callback = CrosspointStatusCallback;
			if (callback != null)
				callback((ushort)args.Data);
		}

		/// <summary>
		/// Send output data from the crosspoint into S+
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="data"></param>
		private void CrosspointOnSendOutputData(ICrosspoint sender, CrosspointData data)
		{
			m_ReceiveSection.Enter();

			try
			{
				if (SmartObjectId == 0 || data.MessageType == CrosspointData.eMessageType.Ping || data.MessageType == CrosspointData.eMessageType.Pong)
					return;

				//Only pass along SmartObject that matches
				foreach (SigInfo sig in data.GetSigs().Where(sig => sig.SmartObject == SmartObjectId))
				{
					ProcessOutputSig(sig);
				}
			}
			finally
			{
				m_ReceiveSection.Leave();
			}
		}

		#endregion

		/// <summary>
		/// Processes single output sigs to go to S+
		/// Override to handle special cases for output sigs
		/// Offset value is added to the sig number
		/// Use a negative offset to remove from the sig number
		/// </summary>
		/// <param name="sig"></param>
		/// <param name="offset"></param>
		protected virtual void ProcessOutputSig(SigInfo sig)
		{
			SendSigToSPlus(sig, 0);
		}

		/// <summary>
		/// Processes single output sigs to go to S+
		/// Override to handle special cases for output sigs
		/// Offset value is added to the sig number
		/// Use a negative offset to remove from the sig number
		/// </summary>
		/// <param name="sig"></param>
		/// <param name="offset"></param>
		protected void SendSigToSPlus(SigInfo sig, int offset)
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
				handler(sig.ToXSig(offset));
		}

		private IControlCrosspoint GetCrosspoint(int crosspointId)
		{
			if (Manager == null || crosspointId == 0)
				return null;

			IControlCrosspoint crosspoint;

			if (Manager.TryGetCrosspoint(crosspointId, out crosspoint))
				return crosspoint;
			
			return null;
		}

		private void InvokeCrosspointChangedCallback()
		{
			SPlusCrosspointChangedCallback callback = CrosspointChangedCallback;
			if (callback != null)
				callback();
		}

		/// <summary>
		/// Adds input sig data to the XP3 after the buffer receives a complete xsig
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="stringEventArgs"></param>
		private void BufferOnCompletedSerial(object sender, StringEventArgs stringEventArgs)
		{
			if (Crosspoint == null || SmartObjectId == 0)
				return;

			SigInfo sig = GetSigInfoForXSig(stringEventArgs.Data);
			CrosspointData data = new CrosspointData();
			data.AddSig(sig);

			Crosspoint.SendInputData(data);
		}

		/// <summary>
		/// Converts XSig into SigInfo
		/// Override to modify default behavior, ie apply offset
		/// </summary>
		/// <param name="xsig"></param>
		/// <returns></returns>
		protected virtual SigInfo GetSigInfoForXSig(string xsig)
		{
			return XSigParser.Parse(xsig).ToSigInfo(SmartObjectId);
		}

		/// <summary>
		/// Update the crosspoint status appropriately
		/// </summary>
		private void UpdateCrosspointStatus()
		{
			SPlusStatusUpdateCallback callback = CrosspointStatusCallback;
			if (callback == null)
				return;

			if (Crosspoint == null)
				callback(0);
			else
				callback((ushort)Crosspoint.Status);
		}
	}
}