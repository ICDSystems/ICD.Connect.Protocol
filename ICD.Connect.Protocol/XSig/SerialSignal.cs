using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ICD.Connect.Protocol.XSig
{
	public sealed class SerialSignal : ISignal<string>
	{
		private readonly BitArray m_Data;

		#region Properties

		/// <summary>
		/// Gets the raw signal data.
		/// </summary>
		public byte[] Data { get { return m_Data.ToBytes(); } }

		/// <summary>
		/// Gets the serial value.
		/// </summary>
		public string Value { get { return GetValue(m_Data); } }

		/// <summary>
		/// Gets the signal index.
		/// </summary>
		public ushort Index { get { return GetIndex(m_Data); } }

		#endregion

		#region Constructors

		/// <summary>
		/// Instantiates the SerialSignal from a collection of bytes.
		/// </summary>
		/// <param name="bytes"></param>
		public SerialSignal(IEnumerable<byte> bytes)
		{
			byte[] array = bytes.ToArray();

			if (!IsSerial(array))
				throw new ArgumentException();

			m_Data = new BitArray(array);
		}

		/// <summary>
		/// Instantiates the SerialSignal from a string and a signal index.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="index"></param>
		public SerialSignal(string value, ushort index)
		{
			m_Data = new BitArray(24 + 8 * value.Length);

			SetIndex(m_Data, index);
			SetValue(m_Data, value);

			for (int bitIndex = 0; bitIndex < 8; bitIndex++)
				m_Data[bitIndex] = true;

			m_Data[m_Data.Length - 1] = true;
			m_Data[m_Data.Length - 2] = true;
			m_Data[m_Data.Length - 3] = false;
			m_Data[m_Data.Length - 4] = false;
			m_Data[m_Data.Length - 5] = true;
			m_Data[m_Data.Length - 9] = false;
		}

		#endregion

		/// <summary>
		/// Returns true if the given data is a serial signature.
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns></returns>
		public static bool IsSerial(IEnumerable<byte> bytes)
		{
			byte[] array = bytes.ToArray();

			if (array.Length < 3)
				return false;

			// Last byte is 1111 1111
			if (array[0] != 0xFF)
				return false;

			BitArray bits = new BitArray(array);

			// Leads with 11001###0...
			return bits[bits.Length - 1]
				   && bits[bits.Length - 2]
				   && !bits[bits.Length - 3]
				   && !bits[bits.Length - 4]
				   && bits[bits.Length - 5]
				   && !bits[bits.Length - 9];
		}

		#region Private Methods

		/// <summary>
		/// Sets the string value to the data BitArray.
		/// </summary>
		/// <param name="array"></param>
		/// <param name="value"></param>
		private static void SetValue(BitArray array, string value)
		{
			byte[] chars = Encoding.GetEncoding(28591).GetBytes(value);
			for (int index = 0; index < value.Length; index++)
			{
				int byteIndex = array.GetBytesLength() - (3 + index);
				array.SetByte(byteIndex, chars[index]);
			}
		}

		/// <summary>
		/// Gets the string value from the data BitArray.
		/// </summary>
		/// <param name="array"></param>
		public static string GetValue(BitArray array)
		{
			string output = string.Empty;

			for (int index = array.GetBytesLength() - 3; index > 0; index--)
				output = output + (char)array.GetByte(index);

			return output;
		}

		/// <summary>
		/// Sets the index bits into the bit array.
		/// </summary>
		/// <param name="array"></param>
		/// <param name="index"></param>
		private static void SetIndex(BitArray array, ushort index)
		{
			byte[] bytes = BitConverter.GetBytes(index);
			BitArray indexBits = new BitArray(bytes);

			indexBits.Insert(7, false);
			indexBits.Insert(11, false);

			for (int bitIndex = 0; bitIndex < indexBits.Length; bitIndex++)
			{
				int arrayIndex = (array.Length - 16) + bitIndex;
				array[arrayIndex] = indexBits[bitIndex] || array[arrayIndex];
			}
		}

		/// <summary>
		/// Gets the index bits out of the bit array.
		/// </summary>
		/// <param name="array"></param>
		/// <returns></returns>
		public static ushort GetIndex(BitArray array)
		{
			byte[] bytes = array.ToBytes();
			byte[] indexBytes = bytes.Skip(bytes.Length - 2).ToArray();

			BitArray temp = new BitArray(indexBytes);

			temp.Remove(11, 5);
			temp.Remove(7);

			return temp.ToUShort();
		}

		#endregion
	}
}
