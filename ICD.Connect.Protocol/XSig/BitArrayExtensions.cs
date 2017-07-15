using System;
using System.Collections;

namespace ICD.Connect.Protocol.XSig
{
	/// <summary>
	/// Extension methods for working with BitArrays.
	/// </summary>
	public static class BitArrayExtensions
	{
		/// <summary>
		/// Shifts the bits >= index to the left and sets the index value.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="index"></param>
		/// <param name="value"></param>
		public static void Insert(this BitArray extends, int index, bool value)
		{
			for (int bitIndex = extends.Length - 1; bitIndex > index; bitIndex--)
				extends[bitIndex] = extends[bitIndex - 1];
			extends[index] = value;
		}

		/// <summary>
		/// Inserts the value the given number of times.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="index"></param>
		/// <param name="value"></param>
		/// <param name="count"></param>
		public static void Insert(this BitArray extends, int index, bool value, int count)
		{
			for (int countIndex = 0; countIndex < count; countIndex++)
				Insert(extends, index, value);
		}

		/// <summary>
		/// Shifts the bits > index to the right.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="index"></param>
		public static void Remove(this BitArray extends, int index)
		{
			for (int bitIndex = index; bitIndex < extends.Length - 1; bitIndex++)
				extends[bitIndex] = extends[bitIndex + 1];
			extends[extends.Length - 1] = false;
		}

		/// <summary>
		/// Removes the value at the index the given number of times.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="index"></param>
		/// <param name="count"></param>
		public static void Remove(this BitArray extends, int index, int count)
		{
			for (int countIndex = 0; countIndex < count; countIndex++)
				Remove(extends, index);
		}

		/// <summary>
		/// Returns the number of bytes that would hold this BiArray (e.g. 6 bits fit in 1 byte).
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static int GetBytesLength(this BitArray extends)
		{
			return (int)Math.Ceiling((double)extends.Length / 8);
		}

		/// <summary>
		/// Converts the BitArray to bytes.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static byte[] ToBytes(this BitArray extends)
		{
			int length = extends.GetBytesLength();
			byte[] byteArray = new byte[length];
			extends.CopyTo(byteArray, 0);
			return byteArray;
		}

		/// <summary>
		/// Gets the byte at the given byte index from the BitArray.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		public static byte GetByte(this BitArray extends, int index)
		{
			return extends.ToBytes()[index];
		}

		/// <summary>
		/// Sets the byte at the given byte index.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="index"></param>
		/// <param name="value"></param>
		public static void SetByte(this BitArray extends, int index, byte value)
		{
			BitArray temp = new BitArray(new []{value});
			int destIndex = index * 8;

			for (int bitIndex = 0; bitIndex < 8; bitIndex++)
				extends[destIndex + bitIndex] = temp[bitIndex];
		}

		/// <summary>
		/// Converts the BitArray to a UShort.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static ushort ToUShort(this BitArray extends)
		{
			return BitConverter.ToUInt16(extends.ToBytes(), 0);
		}

		/// <summary>
		/// Converts the BitArray to a UShort.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static uint ToUInt(this BitArray extends)
		{
			return BitConverter.ToUInt32(extends.ToBytes(), 0);
		}

		/// <summary>
		/// Returns the BitArray in the format 1010101...
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static string ToBinaryString(this BitArray extends)
		{
			string output = string.Empty;

			for (int index = 0; index < extends.Length; index++)
			{
				if (index % 8 == 0 && index != 0)
					output += ' ';
				output += extends[index] ? '1' : '0';
			}

			return string.Format("{0}({1})", extends.GetType().Name, output);
		}

		/// <summary>
		/// Performs an OR operation without concern for length.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="other"></param>
		/// <returns></returns>
		public static void SafeOr(this BitArray extends, BitArray other)
		{
			for (int index = 0; index < Math.Min(extends.Length, other.Length); index++)
				extends[index] |= other[index];
		}
	}
}
