using System;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.Crosspoints.SimplPlus.CrosspointShims;
using ICD.Connect.Protocol.Sigs;
using ICD.Connect.Protocol.XSig;

namespace ICD.Connect.Protocol.Crosspoints.SimplPlus.CrosspointSmartObjectShims
{
	public delegate void SPlusUShortCallback(ushort data);

	[PublicAPI("S+")]
	public sealed class SrlSimplPlusXSigControlCrosspointSoShim : AbstractSimplPlusXSigCrosspointSoShim
	{

		private const ushort SCROLL_TO_ITEM_JOIN = 2;
		private const ushort DIGITAL_IS_MOVING_JOIN = 1;
		private const ushort NUMBER_OF_ITEMS_JOIN = 3;
		private const ushort ENABLE_START_JOIN = 11;
		private const ushort VISIBLE_START_JOIN = 2011;
		private const ushort START_DIGITAL_JOIN = 4011;
		private const ushort START_ANALOG_JOIN = 11;
		private const ushort START_SERIAL_JOIN = 11;
		private const ushort MAX_ITEMS = 2000;
		private const int OFFSET_NEGATIVE = -1;

		#region Callbacks

		[PublicAPI("S+")]
		public SPlusJoinXSigCallback DigitalSrlEnableCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusJoinXSigCallback DigitalSrlVisibleCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusUShortCallback ScrollToItemCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusUShortCallback SetNumberOfItemsCallback { get; set; }

		#endregion

		#region SPlus Methods

		[PublicAPI("S+")]
		public void SendIsMoving(ushort moving)
		{
			if (Crosspoint == null || SmartObjectId == 0)
				return;

			SigInfo sig = new SigInfo(DIGITAL_IS_MOVING_JOIN, SmartObjectId, moving.ToBool());

			CrosspointData data = new CrosspointData();
			data.AddSig(sig);

			Crosspoint.SendInputData(data);
		}

		#endregion

		/// <summary>
		/// Processes single output sigs to go to S+
		/// Override to handle special cases for output sigs
		/// </summary>
		/// <param name="sig"></param>
		protected override void ProcessOutputSig(SigInfo sig)
		{
			// Handle standard join range sigs with offset
			// These are the sigs to/from the SRL pages
			if (IsSigInStandardJoinRange(sig))
			{
				SendSigToSPlus(sig, GetOffsetForType(sig.Type) * OFFSET_NEGATIVE);
				return;
			}

			if (IsSigSrlEnable(sig))
			{
				SendSigAsXSig(DigitalSrlEnableCallback, sig, ENABLE_START_JOIN);
				return;
			}

			if (IsSigSrlVisible(sig))
			{
				SendSigAsXSig(DigitalSrlVisibleCallback, sig, VISIBLE_START_JOIN);
			}

			if (sig.Type == eSigType.Analog && sig.Number == SCROLL_TO_ITEM_JOIN)
			{
				SPlusUShortCallback handler = ScrollToItemCallback;
				if (handler != null)
					handler(sig.GetUShortValue());
				return;
			}

			if (sig.Type == eSigType.Analog && sig.Number == NUMBER_OF_ITEMS_JOIN)
			{
				SPlusUShortCallback handler = SetNumberOfItemsCallback;
				if (handler != null)
					handler(sig.GetUShortValue());
				return;
			}
		}

		/// <summary>
		/// Converts XSig into SigInfo
		/// Override to modify default behavior, ie apply offset
		/// </summary>
		/// <param name="xsig"></param>
		/// <returns></returns>
		protected override SigInfo GetSigInfoForXSig(string xsig)
		{
			IXSig sig = XSigParser.Parse(xsig);
			eSigType sigType;

			if (sig is DigitalXSig)
				sigType = eSigType.Digital;
			else if (sig is AnalogXSig)
				sigType = eSigType.Analog;
			else if (sig is SerialXSig)
				sigType = eSigType.Serial;
			else
				throw new InvalidOperationException("Could not determine xsig type");


			return sig.ToSigInfo(SmartObjectId, GetOffsetForType(sigType));
		}

		private static int GetOffsetForType(eSigType type)
		{
			switch (type)
			{
				case eSigType.Digital:
					return START_DIGITAL_JOIN - 1;
				case eSigType.Analog:
					return START_ANALOG_JOIN - 1;
				case eSigType.Serial:
					return START_SERIAL_JOIN - 1;
				default:
					throw new ArgumentException("eSigType is not recognized");
			}
		}

		private static bool IsSigInStandardJoinRange(SigInfo sig)
		{
			switch (sig.Type)
			{
				case eSigType.Digital:
					return sig.Number >= START_DIGITAL_JOIN;
				case eSigType.Analog:
					return sig.Number >= START_ANALOG_JOIN;
				case eSigType.Serial:
					return sig.Number >= START_SERIAL_JOIN;
			}
			return false;
		}

		private static bool IsSigSrlVisible(SigInfo sig)
		{
			if (sig.Type == eSigType.Digital &&
			    sig.Number >= VISIBLE_START_JOIN &&
			    sig.Number < (VISIBLE_START_JOIN + MAX_ITEMS))
				return true;
			return false;
		}

		private bool IsSigSrlEnable(SigInfo sig)
		{
			if (sig.Type == eSigType.Digital &&
				sig.Number >= ENABLE_START_JOIN &&
				sig.Number < (ENABLE_START_JOIN + MAX_ITEMS))
					return true;
			return false;
		}

		/// <summary>
		/// Sends the sigs to S+ via the specified handler
		/// Uses the start join to caculate offset
		/// Offset = (startJoin - 1) * -1
		/// </summary>
		/// <param name="handler"></param>
		/// <param name="sig"></param>
		/// <param name="startJoin"></param>
		private static void SendSigAsXSig(SPlusJoinXSigCallback handler, SigInfo sig, int startJoin)
		{
			if (handler == null)
				return;
			handler(sig.ToXSig((startJoin - 1) * OFFSET_NEGATIVE));
		}
	}
}