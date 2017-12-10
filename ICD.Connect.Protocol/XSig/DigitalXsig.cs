using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.Sigs;

namespace ICD.Connect.Protocol.XSig
{
	/// <summary>
	/// Represents the data structure of a digital Crestron XSIG in the format:
	///		1 0 value # # # # #    0 # # # # # # #
	/// Where the 12 bit index is spread over the #s.
	/// </summary>
	public struct DigitalXSig : IXSig<bool>
	{
		private readonly byte[] m_Data;

		#region Properties

		/// <summary>
		/// Gets the raw signal data.
		/// </summary>
		public byte[] Data { get { return m_Data.ToArray(); } }

		/// <summary>
		/// Gets the signal data in xsig formatted string
		/// </summary>
		public string DataXSig
		{
			get
			{
				return new string(m_Data.Select(b => (char)b).ToArray());
			}
		}

		/// <summary>
		/// Gets the high/low signal value.
		/// </summary>
		public bool Value { get { return GetValue(); } }

		/// <summary>
		/// Gets the signal index.
		/// </summary>
		public ushort Index { get { return GetIndex(); } }

		#endregion

		#region Constructors

		/// <summary>
		/// Instantiates the DigitalSignal from a collection of bytes.
		/// </summary>
		/// <param name="bytes"></param>
		public DigitalXSig(IEnumerable<byte> bytes)
		{
			byte[] array = bytes.ToArray();

			if (!IsDigital(array))
				throw new ArgumentException("Byte array is not a digital xsig");

			m_Data = array;
		}

		/// <summary>
		/// Instantiates the DigitalSignal from a high/low signal and a signal index.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="index"></param>
		public DigitalXSig(bool value, ushort index)
		{
			if (index >= (1 << 12))
				throw new ArgumentException("Index must be between 0 and 4095");

			m_Data = new byte[2];

			SetFixedBits();
			SetIndex(index);
			SetValue(value);
		}

		#endregion

		/// <summary>
		/// Returns true if the bytes represent a digital signal.
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns></returns>
		public static bool IsDigital(IEnumerable<byte> bytes)
		{
			byte[] array = bytes.ToArray();

			if (array.Length != 2)
				return false;

			// One bit is always true and two bits are always false.
			return array[0].GetBit(7) && !array[0].GetBit(6) && !array[1].GetBit(7);
		}

		/// <summary>
		/// Returns true if the given, potentially incomplete data could represent a digital sig.
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns></returns>
		public static bool IsDigitalIncomplete(IEnumerable<byte> bytes)
		{
			byte[] array = bytes.ToArray();

			if (array.Length == 0 || array.Length > 2)
				return false;

			if (!array[0].GetBit(7) || array[0].GetBit(6))
				return false;

			return array.Length != 2 || !array[1].GetBit(7);
		}

		public override string ToString()
		{
			ReprBuilder builder = new ReprBuilder(this);

			builder.AppendProperty("Index", Index);
			builder.AppendProperty("Value", Value);

			return builder.ToString();
		}

		/// <summary>
		/// Convert XSig to SigInfo
		/// Uses SmartObjectId of 0
		/// </summary>
		/// <returns>SigInfo for the XSig</returns>
		public SigInfo ToSigInfo()
		{
			return ToSigInfo(0);
		}

		/// <summary>
		/// Convert XSig to SigInfo
		/// Using the given SmartObjectId
		/// </summary>
		/// <param name="smartObjectId">SmartObjectId</param>
		/// <returns>SigInfo for the XSig</returns>
		public SigInfo ToSigInfo(ushort smartObjectId)
		{
			return new SigInfo(Index, smartObjectId, Value);
		}

		#region Private Methods

		private void SetFixedBits()
		{
			m_Data[0] = m_Data[0]
				.SetBitOn(7)
				.SetBitOff(6);
			m_Data[1] = m_Data[1].SetBitOff(7);
		}

		/// <summary>
		/// Sets the index bits into the bit array.
		/// </summary>
		/// <param name="index"></param>
		private void SetIndex(ushort index)
		{
			byte[] iBytes = BitConverter.GetBytes(index);
			m_Data[1] = iBytes[0].SetBitOff(7);
			m_Data[0] = m_Data[0]
				.SetBit(0, iBytes[0].GetBit(7))
				.SetBit(1, iBytes[1].GetBit(0))
				.SetBit(2, iBytes[1].GetBit(1))
				.SetBit(3, iBytes[1].GetBit(2))
				.SetBit(4, iBytes[1].GetBit(3));
		}

		/// <summary>
		/// Gets the index bits out of the bit array.
		/// </summary>
		/// <returns></returns>
		private ushort GetIndex()
		{
			byte[] index = new byte[2];
			index[0] = m_Data[1].SetBit(7, m_Data[0].GetBit(0));
			index[1] = ((byte)(m_Data[0] >> 1))
				.SetBitOff(7)
				.SetBitOff(6)
				.SetBitOff(5)
				.SetBitOff(4);
			return BitConverter.ToUInt16(index, 0);
		}

		private bool GetValue()
		{
			return !m_Data[0].GetBit(5);
		}

		private void SetValue(bool value)
		{
			m_Data[0] = m_Data[0].SetBit(5, !value);
		}

		#endregion
	}
}
