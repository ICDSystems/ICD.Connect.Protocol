using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ICD.Connect.Protocol.XSig
{
	/// <summary>
	/// Represents the data structure of an analog Crestron XSIG in the format:
	///		1 1 a a 0 # # #    0 # # # # # # #
	/// 	0 a a a a a a a    0 a a a a a a a
	/// Where the 10 bit index is spread over the #'s.
	/// And the 16 bit value is spread over the a's.
	/// </summary>
	public sealed class AnalogSignal : ISignal<ushort>
	{
		private readonly BitArray m_Data;

		#region Properties

		/// <summary>
		/// Gets the raw signal data.
		/// </summary>
		public byte[] Data { get { return m_Data.ToBytes(); } }

		/// <summary>
		/// Gets the analog signal value.
		/// </summary>
		public ushort Value { get { return GetValue(m_Data); } }

		/// <summary>
		/// Gets the signal index.
		/// </summary>
		public ushort Index { get { return GetIndex(m_Data); } }

		#endregion

		#region Constructors

		/// <summary>
		/// Instantiates the AnalogSignal from a collection of bytes.
		/// </summary>
		/// <param name="bytes"></param>
		public AnalogSignal(IEnumerable<byte> bytes)
		{
			byte[] array = bytes.ToArray();

			if (!IsAnalog(array))
				throw new ArgumentException();

			m_Data = new BitArray(array);
		}

		/// <summary>
		/// Instantiates the AnalogSignal from an analog signal and a signal index.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="index"></param>
		public AnalogSignal(ushort value, ushort index)
		{
			m_Data = new BitArray(32);

			SetIndex(m_Data, index);
			SetValue(m_Data, value);

			m_Data[31] = true;
			m_Data[30] = true;
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

			BitArray bits = new BitArray(array);

			// Two bits are always true and fours bits are always false.
			return bits[31] && bits[30] && !bits[27] && !bits[23] && !bits[15] && !bits[7];
		}

		#region Private Methods

		/// <summary>
		/// Sets the ushort value to the data BitArray.
		/// </summary>
		/// <param name="array"></param>
		/// <param name="value"></param>
		private static void SetValue(BitArray array, ushort value)
		{
			byte[] bytes = BitConverter.GetBytes((uint)value);
			BitArray valueBits = new BitArray(bytes);

			valueBits.Insert(7, false);
			valueBits.Insert(15, false, 13);

			array.SafeOr(valueBits);
		}

		/// <summary>
		/// Gets the ushort value from the data BitArray.
		/// </summary>
		/// <param name="array"></param>
		private static ushort GetValue(BitArray array)
		{
			BitArray temp = new BitArray(array);

			temp.Remove(15, 13);
			temp.Remove(7);

			for (int index = 16; index < temp.Length; index++)
				temp[index] = false;

			return temp.ToUShort();
		}

		/// <summary>
		/// Sets the index bits into the bit array.
		/// </summary>
		/// <param name="array"></param>
		/// <param name="index"></param>
		private static void SetIndex(BitArray array, ushort index)
		{
			byte[] bytes = BitConverter.GetBytes((uint)index);
			BitArray indexBits = new BitArray(bytes);

			indexBits.Insert(7, false);
			indexBits.Insert(0, false, 16);

			array.SafeOr(indexBits);
		}

		/// <summary>
		/// Gets the index bits out of the bit array.
		/// </summary>
		/// <param name="array"></param>
		/// <returns></returns>
		private static ushort GetIndex(BitArray array)
		{
			BitArray temp = new BitArray(array);

			temp.Remove(0, 16);
			temp.Remove(7);

			for (int index = 10; index < temp.Length; index++)
				temp[index] = false;

			return temp.ToUShort();
		}

		#endregion
	}
}
