using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ICD.Connect.Protocol.XSig
{
	/// <summary>
	/// Represents the data structure of a digital Crestron XSIG in the format:
	///		1 0 value # # # # #    0 # # # # # # #
	/// Where the 12 bit index is spread over the #s.
	/// </summary>
	public struct DigitalSignal : ISignal<bool>
	{
		private const int SIGNIFICANT_BIT = 15;
		private const int VALUE_BIT = 13;
		private const int HIGH_ORDER_BIT = 7;

		private readonly BitArray m_Data;

		#region Properties

		/// <summary>
		/// Gets the raw signal data.
		/// </summary>
		public byte[] Data { get { return m_Data.ToBytes(); } }

		/// <summary>
		/// Gets the high/low signal value.
		/// </summary>
		public bool Value { get { return !m_Data[VALUE_BIT]; } }

		/// <summary>
		/// Gets the signal index.
		/// </summary>
		public ushort Index { get { return GetIndex(m_Data); } }

		#endregion

		#region Constructors

		/// <summary>
		/// Instantiates the DigitalSignal from a collection of bytes.
		/// </summary>
		/// <param name="bytes"></param>
		public DigitalSignal(IEnumerable<byte> bytes)
		{
			byte[] array = bytes.ToArray();

			if (!IsDigital(array))
				throw new ArgumentException();

			m_Data = new BitArray(array);
		}

		/// <summary>
		/// Instantiates the DigitalSignal from a high/low signal and a signal index.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="index"></param>
		public DigitalSignal(bool value, ushort index)
		{
			m_Data = new BitArray(16);

			SetIndex(m_Data, index);

			m_Data[SIGNIFICANT_BIT] = true;
			m_Data[VALUE_BIT] = !value;
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

			BitArray bits = new BitArray(array);

			// One bit is always true and two bits are always false.
			return bits[15] && !bits[14] && !bits[7];
		}

		#region Private Methods

		/// <summary>
		/// Sets the index bits into the bit array.
		/// </summary>
		/// <param name="array"></param>
		/// <param name="index"></param>
		private static void SetIndex(BitArray array, ushort index)
		{
			byte[] bytes = BitConverter.GetBytes(index);
			BitArray indexBits = new BitArray(bytes);

			indexBits.Insert(HIGH_ORDER_BIT, false);

			for (int bitIndex = 0; bitIndex < VALUE_BIT; bitIndex++)
				array[bitIndex] = indexBits[bitIndex];
		}

		/// <summary>
		/// Gets the index bits out of the bit array.
		/// </summary>
		/// <param name="array"></param>
		/// <returns></returns>
		private static ushort GetIndex(BitArray array)
		{
			BitArray temp = new BitArray(array);

			for (int index = SIGNIFICANT_BIT; index >= VALUE_BIT; index--)
				temp[index] = false;
			temp.Remove(HIGH_ORDER_BIT);

			return temp.ToUShort();
		}

		#endregion
	}
}
