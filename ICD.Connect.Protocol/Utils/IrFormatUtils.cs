#if SIMPLSHARP
using Crestron.SimplSharp.CrestronIO;
#else
using System.IO;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.IO;

namespace ICD.Connect.Protocol.Utils
{
	/// <summary>
	/// Features for converting between IR driver formats.
	/// </summary>
	public static class IrFormatUtils
	{
		private const byte FIELD_FILE_TYPE = 0xF0;
		private const byte FIELD_HEADER_END = 0xFF;

		/// <summary>
		/// Reads the file at the given path and converts to GC format.
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		public static IEnumerable<string> ReadIrFile(string filename)
		{
			if (!IcdFile.Exists(filename))
				throw new FileNotFoundException(string.Format("No file at path {0}", filename));

			string ext = IcdPath.GetExtension(filename).ToLower();

			switch (ext)
			{
				case ".ir":
					return ReadCrestronIrFile(filename);

				case ".ccf":
					return ReadCcfTextIrFile(filename);

				default:
					throw new FormatException(string.Format("{0} is not a supported extension", ext));
			}
		}

		/// <summary>
		/// Reads the Crestron driver at the given path to GC format.
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		private static IEnumerable<string> ReadCrestronIrFile(string filename)
		{
			if (!IcdFile.Exists(filename))
				throw new FileNotFoundException(string.Format("No file at path {0}", filename));

			using (BinaryReader b = new BinaryReader(File.Open(filename, FileMode.Open)))
			{
				int pos = 1;
				int length = (int)b.BaseStream.Length;

				while (pos < length)
				{
					int fieldLength = b.ReadByte();
					byte fieldType = b.ReadByte();

					if (fieldType >= FIELD_FILE_TYPE && fieldType <= FIELD_HEADER_END)
					{
						byte[] bytearray = b.ReadBytes(fieldLength - 2);
						string tmpSt = System.Text.Encoding.ASCII.GetString(bytearray, 0, bytearray.Length);
						pos += fieldLength;

						switch (fieldType)
						{
							case FIELD_FILE_TYPE:
								if (!tmpSt.Equals("IR"))
									yield break;
								break;

							case FIELD_HEADER_END:
								// Read IR Command data
								while (pos < length)
								{
									fieldLength = b.ReadByte();
									b.ReadByte();
									yield return CrestronToGc(b.ReadBytes(fieldLength - 2));
									pos += fieldLength;
								}
								break;
						}
					}
					else if (fieldType < 0xF0)
					{
						b.ReadBytes(fieldLength - 2);
						pos += fieldLength;
					}
				}
			}
		}

		/// <summary>
		/// Reads the CCF driver at the given path to GC format.
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		private static IEnumerable<string> ReadCcfTextIrFile(string filename)
		{
			if (!IcdFile.Exists(filename))
				throw new FileNotFoundException(string.Format("No file at path {0}", filename));

			using (FileStream stream = new FileStream(filename, FileMode.Open))
			{
				using (StreamReader reader = new StreamReader(stream))
				{
					string line;
					while ((line = reader.ReadLine()) != null)
					{
						line = line.Trim();
						if (CcfValid(line))
							yield return CcfToGc(line);
					}
				}
			}
		}

		#region Conversion

		/// <summary>
		/// Converts Crestron to GC format.
		/// </summary>
		/// <param name="crestronIr"></param>
		/// <returns></returns>
		public static string CrestronToGc(byte[] crestronIr)
		{
			int[] indexedInteger = new int[15];
			int dataOneTimeIndexed = crestronIr[3] & 0x0F;

			int dataLength = crestronIr.Length - 4;
			byte[] newArray = new byte[dataLength];

			Array.Copy(crestronIr, 4, newArray, 0, newArray.Length);

			int dataCcfFreq = 4000000 / crestronIr[0];
			string gc = string.Empty + dataCcfFreq + ",1,1";

			for (int y = 0; y < dataOneTimeIndexed; y++)
				indexedInteger[y] = (newArray[y * 2] << 8) + newArray[1 + (y * 2)];

			for (int y = 0; y < dataLength - (dataOneTimeIndexed * 2); y++)
			{
				int indexHighByte = (newArray[(dataOneTimeIndexed * 2) + y] & 0xF0) >> 4;
				int indexLowByte = newArray[(dataOneTimeIndexed * 2) + y] & 0x0F;
				gc += "," + indexedInteger[indexHighByte] + "," + indexedInteger[indexLowByte];
			}

			return gc;
		}

		/// <summary>
		/// Converts CCF to GC format.
		/// </summary>
		/// <param name="ccf"></param>
		/// <returns></returns>
		public static string CcfToGc(string ccf)
		{
			if (ccf == null)
				throw new ArgumentNullException("ccf");

			int[] intArray = ccf.Split(' ')
								.Select(s => int.Parse(s, System.Globalization.NumberStyles.HexNumber))
								.ToArray();

			int length = intArray.Length;

			int num = (((0xa1ea / intArray[1]) + 5) / 10) * 0x3e8;

			string gc = num + ",1,1";
			for (int i = 4; i < length; i++)
				gc += "," + intArray[i];

			return gc;
		}

		#endregion

		/// <summary>
		/// Returns true if the given CCF string is valid.
		/// </summary>
		/// <param name="ccf"></param>
		/// <returns></returns>
		public static bool CcfValid(string ccf)
		{
			if (ccf == null)
				throw new ArgumentNullException("ccf");

			int length = ccf.Length;
			if (length < 0x1d || length > 0x513)
				return false;

			return ccf.ToCharArray().All(CcfValid);
		}

		/// <summary>
		/// Returns true if the given CCF character is valid.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public static bool CcfValid(char input)
		{
			return ((input >= '0') && (input <= '9')) ||
			       ((input >= 'a') && (input <= 'f')) ||
			       (((input >= 'A') && (input <= 'F')) ||
			        (input == ' '));
		}
	}
}
