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
				// Crestron
				case ".ir":
					return ImportCrestronDriverFromPath(path);
				// Krang
				case ".csv":
					return ImportCsvDriverFromPath(path);
				/*
				// TODO - support Pronto files
				// Pronto
				case ".ccf":
					return ImportProntoDriverFromPath(path);
				*/

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
				Offset = false,
				Data = data
			};
		}

		#endregion

		#region Csv

		/// <summary>
		/// Custom format - each line should be the IR code name followed by the hex describing the IR Code
		/// Example:
		/// COMMAND NAME,0000 1111 2222
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private static KrangIrDriver ImportCsvDriverFromPath(string path)
		{
			if (!IcdFile.Exists(path))
				throw new FileNotFoundException(string.Format("No file at path {0}", path));

			string content = IcdFile.ReadToEnd(path, Encoding.UTF8);

			IEnumerable<KrangIrCommand> commands =
				content.Split('\r', '\n')
				       .Select(s => s.Trim())
				       .Where(s => !string.IsNullOrEmpty(s))
				       .Select(s =>
				       {
					       string[] entry = s.Split(',');
					       //if (CsvDataEntryValid(entry[1]))
					       return ImportIrCommandFromCsvEntry(entry);
				       });

			KrangIrDriver driver = new KrangIrDriver();
			foreach (KrangIrCommand command in commands)
			{
				driver.AddCommand(command);
			}

			return driver;
		}

		/// <summary>
		/// Converts a csv line entry into an IR command class.
		/// </summary>
		/// <param name="entry"></param>
		/// <returns></returns>
		private static KrangIrCommand ImportIrCommandFromCsvEntry(string[] entry)
		{
			if (entry.Length < 2)
				throw new FormatException("Invalid configured IR command in csv file.");

			string name = entry[0];
			string data = entry[1];

			int[] intArray = data.Split(' ')
			                     .Select(s => int.Parse(s, System.Globalization.NumberStyles.HexNumber))
			                     .ToArray();

			int num = (((0xa1ea / intArray[1]) + 5) / 10) * 0x3e8;

			return new KrangIrCommand
			{
				Name = name,
				Frequency = num,
				RepeatCount = 1,
				Offset = true,
				Data = intArray.Skip(4).ToList()
			};
		}

		/// <summary>
		/// Returns true if the given CCF string is valid.
		/// </summary>
		/// <param name="dataEntry"></param>
		/// <returns></returns>
		private static bool CsvDataEntryValid(string dataEntry)
		{
			if (dataEntry == null)
				throw new ArgumentNullException("dataEntry");

			int length = dataEntry.Length;
			if (length < 0x1d || length > 0x513)
				return false;

			return dataEntry.ToCharArray().All(CsvDataEntryValid);
		}

		/// <summary>
		/// Returns true if the given CCF character is valid.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		private static bool CsvDataEntryValid(char input)
		{
			return ((input >= '0') && (input <= '9')) ||
			       ((input >= 'a') && (input <= 'f')) ||
			       (((input >= 'A') && (input <= 'F')) ||
			        (input == ' '));
		}

		#endregion
	}
}
