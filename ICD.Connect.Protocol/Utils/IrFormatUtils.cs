using ICD.Common.Utils;
#if SIMPLSHARP
using Crestron.SimplSharp.CrestronIO;
#else
using System.IO;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ICD.Common.Properties;
using ICD.Common.Utils.Csv;
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

		#region Regex

		private const string GLOBAL_CACHE_IR_COMMAND_REGEX =
			@"[a-zA-Z]+,\d+:\d+,\d+,(?'freq'\d+),\d+,\d+,(?'data'\d+(,\d+)*)";

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
		/// 3rd column of a csv driver should specify one of these driver types.
		/// </summary>
		private enum eCsvDriverType
		{
			ProntoHex = 1,
			GlobalCache = 2
		}

		/// <summary>
		/// Custom format - See example file in ICD.Connect.Protocol.Utils for details.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private static KrangIrDriver ImportCsvDriverFromPath(string path)
		{
			if (!IcdFile.Exists(path))
				throw new FileNotFoundException(string.Format("No file at path {0}", path));

			List<KrangIrCommand> commands = new List<KrangIrCommand>();
			CsvReaderSettings settings = new CsvReaderSettings
			{
				HeaderRowIncluded = false
			};

			using (var streamReader = new IcdStreamReader(path))
			{
				using (var csvReader = new CsvReader(streamReader, settings))
				{
					commands.AddRange(csvReader.Lines().Select(row => ImportIrCommandFromCsvEntry(row)));
				}
			}

			KrangIrDriver driver = new KrangIrDriver();
			foreach (KrangIrCommand command in commands)
				driver.AddCommand(command);

			return driver;
		}

		/// <summary>
		/// Converts a csv line entry into an IR command class.
		/// </summary>
		/// <param name="entry"></param>
		/// <returns></returns>
		private static KrangIrCommand ImportIrCommandFromCsvEntry(string[] entry)
		{
			if (entry.Length < 3)
				throw new FormatException("Invalid configured IR command in csv file.");

			string name = entry[0];
			string data = entry[1];
			eCsvDriverType type;

			if (!EnumUtils.TryParse(entry[2], true, out type))
				throw new FormatException("Unspecified driver type");

			switch (type)
			{
				case eCsvDriverType.ProntoHex:
					return ImportProntoIrCommand(name, data);
				case eCsvDriverType.GlobalCache:
					return ImportGlobalCacheIrCommand(name, data);
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		#region Pronto Hex

		/// <summary>
		/// Imports the Pronto Hex IR command from a configured csv file.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		private static KrangIrCommand ImportProntoIrCommand([NotNull] string name, [NotNull] string data)
		{
			if (!ProntoHexDataEntryValid(data))
				throw new FormatException("Invalid Pronto Hex");

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
		/// Returns true if the given pronto hex string is valid.
		/// </summary>
		/// <param name="dataEntry"></param>
		/// <returns></returns>
		private static bool ProntoHexDataEntryValid(string dataEntry)
		{
			if (dataEntry == null)
				throw new ArgumentNullException("dataEntry");

			int length = dataEntry.Length;
			if (length < 0x1d || length > 0x513)
				return false;

			return dataEntry.ToCharArray().All(ProntoHexDataEntryValid);
		}

		/// <summary>
		/// Returns true if the given pronto hex character is valid.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		private static bool ProntoHexDataEntryValid(char input)
		{
			return ((input >= '0') && (input <= '9')) ||
			       ((input >= 'a') && (input <= 'f')) ||
			       (((input >= 'A') && (input <= 'F')) ||
			        (input == ' '));
		}

		#endregion

		#region Global Cache

		/// <summary>
		/// Imports the Global Cache IR command string.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		private static KrangIrCommand ImportGlobalCacheIrCommand(string name, string data)
		{
			if (!GlobaclCacheDataEntryValid(data))
				throw new FormatException("Invalid Global Cache IR command string");

			Match match = Regex.Match(data, GLOBAL_CACHE_IR_COMMAND_REGEX);

			int freq = int.Parse(match.Groups["freq"].Value);
			List<int> intData = match.Groups["data"]
			                         .Value
			                         .Split(',')
			                         .Select(s => int.Parse(s))
			                         .ToList();
			bool offset = intData.Count % 2 == 0;

			return new KrangIrCommand
			{
				Name = name,
				RepeatCount = 1,
				Frequency = freq,
				Data = intData,
				Offset = offset
			};
		}

		/// <summary>
		/// Checks if the data entry matches the global cache command string regex.
		/// </summary>
		/// <param name="dataEntry"></param>
		/// <returns></returns>
		private static bool GlobaclCacheDataEntryValid(string dataEntry)
		{
			return Regex.IsMatch(dataEntry, GLOBAL_CACHE_IR_COMMAND_REGEX);
		}

		#endregion

		#endregion
	}
}
