#if SIMPLSHARP
using Crestron.SimplSharp.CrestronIO;
#else
using System.IO;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.IO;
using ICD.Connect.Protocol.Data;

namespace ICD.Connect.Protocol.Utils
{
	/// <summary>
	/// Features for converting between IR driver formats.
	/// </summary>
	public static class IrFormatUtils
	{
		#region Crestron IR file byte delimiters

		private const byte FIELD_FILE_TYPE = 0xF0;
		private const byte FIELD_HEADER_END = 0xFF;

		#endregion

		#region Methods

		/// <summary>
		/// Imports the an IR driver from the specified path.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static KrangIrDriver ImportDriverFromPath(string path)
		{
			if (!IcdFile.Exists(path))
				throw new FileNotFoundException(string.Format("No file at path {0}", path));

			string ext = IcdPath.GetExtension(path).ToLower();

			switch (ext)
			{
				case ".ir":
					return ImportCrestronDriverFromPath(path);
				// TODO Pronto
				case ".ccf":

				// Krang
				case ".csv":
					return ImportCsvDriverFromPath(path);

				default:
					throw new FormatException(string.Format("{0} is not a supported extension", ext));
			}
		}

		#endregion

		#region Crestron

		/// <summary>
		/// Reads the Crestron driver at the given path to GC format.
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		private static KrangIrDriver ImportCrestronDriverFromPath(string filename)
		{
			if (!IcdFile.Exists(filename))
				throw new FileNotFoundException(string.Format("No file at path {0}", filename));

			KrangIrDriver driver = new KrangIrDriver();

			using (BinaryReader b = new BinaryReader(File.Open(filename, FileMode.Open)))
			{
				// Some unwanted fields occur multiple times, so select the last appearance of each field type
				Dictionary<byte, byte[]> header = ReadCrestronDriverHeader(b).Reverse().Distinct(kvp => kvp.Key).ToDictionary();
				Dictionary<byte, byte[]> body = ReadCrestronDriverBody(b).Reverse().Distinct(kvp => kvp.Key).ToDictionary();

				foreach (var kvp in header.OrderBy(k => k.Key))
				{
					byte[] irData;
					body.TryGetValue(kvp.Key, out irData);
					if (irData == null)
						continue;

					driver.AddCommand(CrestronToKrangCommand(irData, Encoding.ASCII.GetString(kvp.Value, 0, kvp.Value.Length)));
				}
			}

			return driver;
		}

		/// <summary>
		/// Reads the header binaries of a Crestron .ir file.
		/// </summary>
		/// <param name="b"></param>
		/// <returns></returns>
		private static IEnumerable<KeyValuePair<byte, byte[]>> ReadCrestronDriverHeader(BinaryReader b)
		{
			while (true)
			{
				int fieldLength = b.ReadByte();
				byte fieldType = b.ReadByte();
				byte[] bytearray = b.ReadBytes(fieldLength - 2);

				switch (fieldType)
				{
					case FIELD_FILE_TYPE:
						string tmpSt = Encoding.ASCII.GetString(bytearray, 0, bytearray.Length);
						if (!tmpSt.Equals("IR"))
							throw new FormatException("Unexpected file type " + tmpSt);
						yield return new KeyValuePair<byte, byte[]>(fieldType, bytearray);
						break;

					case FIELD_HEADER_END:
						yield break;

					default:
						yield return new KeyValuePair<byte, byte[]>(fieldType, bytearray);
						break;
				}
			}
		}

		/// <summary>
		/// Reads the body binaries of a Crestron .ir file.
		/// </summary>
		/// <param name="b"></param>
		/// <returns></returns>
		private static IEnumerable<KeyValuePair<byte, byte[]>> ReadCrestronDriverBody(BinaryReader b)
		{
			while (b.BaseStream.Position < b.BaseStream.Length - 1)
			{
				int fieldLength = b.ReadByte();
				byte fieldType = b.ReadByte();
				byte[] bytearray = b.ReadBytes(fieldLength - 2);

				switch (fieldType)
				{
					default:
						yield return new KeyValuePair<byte, byte[]>(fieldType, bytearray);
						break;
				}
			}
		}

		/// <summary>
		/// Converts Crestron to krang command.
		/// </summary>
		/// <param name="crestronIr"></param>
		/// <param name="commandName"></param>
		/// <returns></returns>
		private static KrangIrCommand CrestronToKrangCommand(byte[] crestronIr, string commandName)
		{
			int[] indexedInteger = new int[15];
			int dataOneTimeIndexed = crestronIr[3] & 0x0F;

			int dataLength = crestronIr.Length - 4;
			byte[] newArray = new byte[dataLength];

			Array.Copy(crestronIr, 4, newArray, 0, newArray.Length);

			int dataCcfFreq = 4000000 / crestronIr[0];

			List<int> data = new List<int> {1};

			for (int y = 0; y < dataOneTimeIndexed; y++)
				indexedInteger[y] = (newArray[y * 2] << 8) + newArray[1 + (y * 2)];

			for (int y = 0; y < dataLength - (dataOneTimeIndexed * 2); y++)
			{
				int indexHighByte = (newArray[(dataOneTimeIndexed * 2) + y] & 0xF0) >> 4;
				int indexLowByte = newArray[(dataOneTimeIndexed * 2) + y] & 0x0F;

				data.Add(indexedInteger[indexHighByte]);
				data.Add(indexedInteger[indexLowByte]);
			}

			return new KrangIrCommand
			{
				Name = commandName,
				Frequency = dataCcfFreq,
				RepeatCount = 1,
				Data = data
			};
		}

		#endregion

		private static KrangIrDriver ImportCsvDriverFromPath(string path)
		{
			if (!IcdFile.Exists(path))
				throw new FileNotFoundException(string.Format("No file at path {0}", path));

			//using (FileStream stream = new FileStream(path, FileMode.Open))
			//{
			//	using (StreamReader reader = new StreamReader(stream))
			//	{
			//		List<string> names = new List<string>();
			//		List<byte[]> bytes = new List<byte[]>();

			//		while (!reader.EndOfStream)
			//		{
			//			var line = reader.ReadLine();
			//			var values = line.Split(',');

			//			names.Add(values[0]);
			//			bytes.Add(values[1]);
			//		}
			//	}
			//}

			return null;
		}

		#region old

		///// <summary>
		///// Reads the file at the given path and converts to GC format.
		///// </summary>
		///// <param name="filename"></param>
		///// <returns></returns>
		//public static IEnumerable<string> ReadIrFileToGc(string filename)
		//{
		//	if (!IcdFile.Exists(filename))
		//		throw new FileNotFoundException(string.Format("No file at path {0}", filename));

		//	string ext = IcdPath.GetExtension(filename).ToLower();

		//	switch (ext)
		//	{
		//		case ".ir":
		//			return ImportCrestronDriverFromPath(filename);

		//		case ".ccf":
		//			return ReadCcfTextIrFile(filename);

		//		default:
		//			throw new FormatException(string.Format("{0} is not a supported extension", ext));
		//	}
		//}

		///// <summary>
		///// Reads the CCF driver at the given path to GC format.
		///// </summary>
		///// <param name="filename"></param>
		///// <returns></returns>
		//private static IEnumerable<string> ReadCcfTextIrFile(string filename)
		//{
		//	if (!IcdFile.Exists(filename))
		//		throw new FileNotFoundException(string.Format("No file at path {0}", filename));

		//	using (FileStream stream = new FileStream(filename, FileMode.Open))
		//	{
		//		using (StreamReader reader = new StreamReader(stream))
		//		{
		//			string line;
		//			while ((line = reader.ReadLine()) != null)
		//			{
		//				line = line.Trim();
		//				if (CcfValid(line))
		//					yield return CcfToGc(line);
		//			}
		//		}
		//	}
		//}

		#region Conversion

		///// <summary>
		///// Converts Crestron to GC format.
		///// </summary>
		///// <param name="crestronIr"></param>
		///// <returns></returns>
		//public static string CrestronToGc(byte[] crestronIr)
		//{
		//	int[] indexedInteger = new int[15];
		//	int dataOneTimeIndexed = crestronIr[3] & 0x0F;

		//	int dataLength = crestronIr.Length - 4;
		//	byte[] newArray = new byte[dataLength];

		//	Array.Copy(crestronIr, 4, newArray, 0, newArray.Length);

		//	int dataCcfFreq = 4000000 / crestronIr[0];
		//	string gc = string.Empty + dataCcfFreq + ",1,1";

		//	for (int y = 0; y < dataOneTimeIndexed; y++)
		//		indexedInteger[y] = (newArray[y * 2] << 8) + newArray[1 + (y * 2)];

		//	for (int y = 0; y < dataLength - (dataOneTimeIndexed * 2); y++)
		//	{
		//		int indexHighByte = (newArray[(dataOneTimeIndexed * 2) + y] & 0xF0) >> 4;
		//		int indexLowByte = newArray[(dataOneTimeIndexed * 2) + y] & 0x0F;
		//		gc += "," + indexedInteger[indexHighByte] + "," + indexedInteger[indexLowByte];
		//	}

		//	return gc;
		//}

		///// <summary>
		///// Converts CCF to GC format.
		///// </summary>
		///// <param name="ccf"></param>
		///// <returns></returns>
		//public static string CcfToGc(string ccf)
		//{
		//	if (ccf == null)
		//		throw new ArgumentNullException("ccf");

		//	int[] intArray = ccf.Split(' ')
		//						.Select(s => int.Parse(s, System.Globalization.NumberStyles.HexNumber))
		//						.ToArray();

		//	int length = intArray.Length;

		//	int num = (((0xa1ea / intArray[1]) + 5) / 10) * 0x3e8;

		//	string gc = num + ",1,1";
		//	for (int i = 4; i < length; i++)
		//		gc += "," + intArray[i];

		//	return gc;
		//}

		#endregion

		///// <summary>
		///// Returns true if the given CCF string is valid.
		///// </summary>
		///// <param name="ccf"></param>
		///// <returns></returns>
		//public static bool CcfValid(string ccf)
		//{
		//	if (ccf == null)
		//		throw new ArgumentNullException("ccf");

		//	int length = ccf.Length;
		//	if (length < 0x1d || length > 0x513)
		//		return false;

		//	return ccf.ToCharArray().All(CcfValid);
		//}

		///// <summary>
		///// Returns true if the given CCF character is valid.
		///// </summary>
		///// <param name="input"></param>
		///// <returns></returns>
		//public static bool CcfValid(char input)
		//{
		//	return ((input >= '0') && (input <= '9')) ||
		//	       ((input >= 'a') && (input <= 'f')) ||
		//	       (((input >= 'A') && (input <= 'F')) ||
		//	        (input == ' '));
		//}

		#endregion
	}
}
