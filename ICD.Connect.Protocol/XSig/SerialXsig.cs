using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.Sigs;

namespace ICD.Connect.Protocol.XSig
{
	public sealed class SerialXSig : IXSig<string>
	{
		private readonly byte[] m_Data;

		#region Properties

		/// <summary>
		/// Gets the raw signal data.
		/// </summary>
		public byte[] Data { get { return m_Data.ToArray(); } }

		/// <summary>
		/// Gets the serial value.
		/// </summary>
		public string Value { get { return GetValue(); } }

		/// <summary>
		/// Gets the signal index.
		/// </summary>
		public ushort Index { get { return GetIndex(); } }

		#endregion

		#region Constructors

		/// <summary>
		/// Instantiates the SerialSignal from a collection of bytes.
		/// </summary>
		/// <param name="bytes"></param>
		public SerialXSig(IEnumerable<byte> bytes)
		{
			byte[] array = bytes.ToArray();

			if (!IsSerial(array))
				throw new ArgumentException("Byte array is not a serial xsig");

			m_Data = array;
		}

		/// <summary>
		/// Instantiates the SerialSignal from a string and a signal index.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="index"></param>
		public SerialXSig(string value, ushort index)
		{
			if (index >= (1 << 10))
				throw new ArgumentException("Index must be between 0 and 1023");
			value = value ?? "";

			m_Data = new byte[value.Length + 3];

			SetFixedBits();
			SetIndex(index);
			SetValue(value);
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
			if (array[array.Length -1] != 0xFF)
				return false;

			// Leads with 11001### 0...
			return array[0].GetBit(7)
			       && array[0].GetBit(6)
			       && !array[0].GetBit(5)
			       && !array[0].GetBit(4)
			       && array[0].GetBit(3)
			       && !array[1].GetBit(7);
		}

		/// <summary>
		/// Returns true if the given, potentially incomplete data could represent a serial sig.
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns></returns>
		public static bool IsSerialIncomplete(IEnumerable<byte> bytes)
		{
			byte[] array = bytes.ToArray();

			if (array.Length == 0)
				return false;

			// Leads with 11001### 0...
			if (!(array[0].GetBit(7)
			      && array[0].GetBit(6)
			      && !array[0].GetBit(5)
			      && !array[0].GetBit(4)
			      && array[0].GetBit(3)))
				return false;

			if (array.Length > 1 && array[1].GetBit(7))
				return false;

			// Ends with a 1111 1111
			int end = array.FindIndex(b => b == 0xFF);
			if (end >= 0 && end != array.Length - 1)
				return false;

			return true;
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

		public override string ToString()
		{
			ReprBuilder builder = new ReprBuilder(this);

			builder.AppendProperty("Index", Index);
			builder.AppendProperty("Value", Value);

			return builder.ToString();
		}

		#region Private Methods

		private void SetFixedBits()
		{
			m_Data[0] = m_Data[0]
					.SetBitOn(7)
					.SetBitOn(6)
					.SetBitOff(5)
					.SetBitOff(4)
					.SetBitOn(3);
			m_Data[1] = m_Data[1].SetBitOff(7);
			m_Data[m_Data.Length - 1] = 0xFF;
		}

		/// <summary>
		/// Sets the string value to the data BitArray.
		/// </summary>
		/// <param name="value"></param>
		private void SetValue(string value)
		{
			byte[] chars = StringUtils.ToBytes(value);
			chars.CopyTo(m_Data, 2);
		}

		/// <summary>
		/// Gets the string value from the data BitArray.
		/// </summary>
		private string GetValue()
		{
			return StringUtils.ToString(m_Data.Skip(2), m_Data.Length - 3);
		}

		/// <summary>
		/// Sets the index bits into the bit array.
		/// </summary>
		/// <param name="index"></param>
		private void SetIndex(ushort index)
		{
			byte[] iBytes = BitConverter.GetBytes(index);
			m_Data[1] = iBytes[0]
					.SetBitOff(7);

			m_Data[0] = m_Data[0]
					.SetBit(0, iBytes[0].GetBit(7))
					.SetBit(1, iBytes[1].GetBit(0))
					.SetBit(2, iBytes[1].GetBit(1));
		}

		/// <summary>
		/// Gets the index bits out of the bit array.
		/// </summary>
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
