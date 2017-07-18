﻿using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Protocol.XSig
{
	/// <summary>
	/// Represents the data structure of an analog Crestron XSIG in the format:
	///		1 1 a a 0 # # #    0 # # # # # # #
	/// 	0 a a a a a a a    0 a a a a a a a
	/// Where the 10 bit index is spread over the #'s.
	/// And the 16 bit value is spread over the a's.
	/// </summary>
	public struct AnalogXsig : IXsig<ushort>
	{
		private readonly byte[] m_Data;

		#region Properties

		/// <summary>
		/// Gets the raw signal data.
		/// </summary>
		public byte[] Data { get { return m_Data.ToArray(); } }

		/// <summary>
		/// Gets the analog signal value.
		/// </summary>
		public ushort Value { get { return GetValue(); } }

		/// <summary>
		/// Gets the signal index.
		/// </summary>
		public ushort Index { get { return GetIndex(); } }

		#endregion

		#region Constructors

		/// <summary>
		/// Instantiates the AnalogSignal from a collection of bytes.
		/// </summary>
		/// <param name="bytes"></param>
		public AnalogXsig(IEnumerable<byte> bytes)
		{
			byte[] array = bytes.ToArray();

			if (!IsAnalog(array))
				throw new ArgumentException("Byte array is not an analog xsig");

			m_Data = array;
		}

		/// <summary>
		/// Instantiates the AnalogSignal from an analog signal and a signal index.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="index"></param>
		public AnalogXsig(ushort value, ushort index)
		{
			if (index >= (1 << 10))
				throw new ArgumentException("index must be between 0 and 1023");

			m_Data = new byte[4];

			SetFixedBits();
			SetIndex(index);
			SetValue(value);
		}

		#endregion

		/// <summary>
		/// Returns true if the given data is an analog signature.
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns></returns>
		public static bool IsAnalog(IEnumerable<byte> bytes)
		{
			byte[] array = bytes.ToArray();

			if (array.Length != 4)
				return false;

			// Two bits are always true and fours bits are always false.
			return array[0].GetBit(7) 
				&& array[0].GetBit(6) 
				&& !array[0].GetBit(3) 
				&& !array[1].GetBit(7) 
				&& !array[2].GetBit(7) 
				&& !array[3].GetBit(7);
		}

		#region Private Methods

		private void SetFixedBits()
		{
			m_Data[0] = m_Data[0]
				.SetBitOn(7)
				.SetBitOn(6)
				.SetBitOff(3);
			m_Data[1] = m_Data[1]
					.SetBitOff(7);
			m_Data[2] = m_Data[2].SetBitOff(7);
			m_Data[3] = m_Data[3].SetBitOff(7);
		}

		/// <summary>
		/// Sets the ushort value to the data BitArray.
		/// </summary>
		/// <param name="array"></param>
		/// <param name="value"></param>
		private void SetValue(ushort value)
		{
			byte[] vBytes = BitConverter.GetBytes(value);

			// set a's in 1 1 a a 0 # # #
			m_Data[0] = m_Data[0]
				.SetBit(5, vBytes[1].GetBit(7))
				.SetBit(4, vBytes[1].GetBit(6));

			// set bytes 3 and 4
			m_Data[2] = ((byte) (vBytes[1] << 1))
				.SetBitOff(7)
				.SetBit(0, vBytes[0].GetBit(7));

			m_Data[3] = vBytes[0].SetBitOff(7);
		}

		/// <summary>
		/// Gets the ushort value from the data BitArray.
		/// </summary>
		/// <param name="array"></param>
		private ushort GetValue()
		{
			byte[] val = new byte[2];
			val[0] = m_Data[3]
				.SetBit(7, m_Data[2].GetBit(0));
			val[1] = ((byte) (m_Data[2] >> 1))
				.SetBit(6, m_Data[0].GetBit(4))
				.SetBit(7, m_Data[0].GetBit(5));

			return BitConverter.ToUInt16(val, 0);
		}

		/// <summary>
		/// Sets the index bits into the bit array.
		/// </summary>
		/// <param name="array"></param>
		/// <param name="index"></param>
		private void SetIndex(ushort index)
		{
			// 1 1 a a 0 # # #    0 # # # # # # #
			byte[] iBytes = BitConverter.GetBytes(index);
			m_Data[1] = iBytes[0].SetBitOff(7);
			m_Data[0] = m_Data[0]
				.SetBit(0, iBytes[0].GetBit(7))
				.SetBit(1, iBytes[1].GetBit(0))
				.SetBit(2, iBytes[1].GetBit(1));
		}

		/// <summary>
		/// Gets the index bits out of the bit array.
		/// </summary>
		/// <param name="array"></param>
		/// <returns></returns>
		private ushort GetIndex()
		{
			byte[] index = new byte[2];

			index[0] = m_Data[1]
				.SetBit(7, m_Data[0].GetBit(0));
			index[1] = index[1]
				.SetBit(0, m_Data[0].GetBit(1))
				.SetBit(1, m_Data[0].GetBit(2));

			return BitConverter.ToUInt16(index, 0);
		}

		#endregion
	}
}