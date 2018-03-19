using System;
using ICD.Common.Utils;
using ICD.Common.Utils.Json;
using ICD.Common.Utils.Xml;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.Utils
{
	public static class DebugUtils
	{
		/// <summary>
		/// Formats and prints the received data to the console.
		/// Does nothing if mode is off.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="mode"></param>
		/// <param name="data"></param>
		public static void PrintRx(object instance, eDebugMode mode, string data)
		{
			PrintRx(instance, mode, null, data);
		}

		/// <summary>
		/// Formats and prints the received data to the console.
		/// Does nothing if mode is off.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="mode"></param>
		/// <param name="context"></param>
		/// <param name="data"></param>
		public static void PrintRx(object instance, eDebugMode mode, string context, string data)
		{
			PrintData(instance, context, data, "RX", mode);
		}

		/// <summary>
		/// Formats and prints the transmitted data to the console.
		/// Does nothing if mode is off.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="mode"></param>
		/// <param name="data"></param>
		public static void PrintTx(object instance, eDebugMode mode, string data)
		{
			PrintTx(instance, mode, null, data);
		}

		/// <summary>
		/// Formats and prints the transmitted data to the console.
		/// Does nothing if mode is off.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="mode"></param>
		/// <param name="context"></param>
		/// <param name="data"></param>
		public static void PrintTx(object instance, eDebugMode mode, string context, string data)
		{
			PrintData(instance, context, data, "TX", mode);
		}

		/// <summary>
		/// Formats and prints the data to the console.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="context"></param>
		/// <param name="data"></param>
		/// <param name="direction"></param>
		/// <param name="mode"></param>
		private static void PrintData(object instance, string context, string data, string direction, eDebugMode mode)
		{
			if (mode == eDebugMode.Off)
				return;

			string modeString = GetModeString(mode);
			
			// Pad context for readability
			context = context == null ? string.Empty : context + " - ";

			// Massage the data
			data = FormatData(data, mode);

			// "Port(Id=1) ClientId:10 - TX(Ascii) - SomeData"
			IcdConsole.Print("{0} {1}{2}({3}) - {4}", instance, context, direction, data, modeString);
			IcdConsole.PrintLine(string.Empty);
		}

		/// <summary>
		/// Formats the data based on the given debug mode.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="mode"></param>
		/// <returns></returns>
		private static string FormatData(string data, eDebugMode mode)
		{
			switch (mode)
			{
				case eDebugMode.Off:
					return null;

				case eDebugMode.Ascii:
					data = data.Replace("\n", "\\n");
					data = data.Replace("\r", "\\r");
					return data;

				case eDebugMode.Hex:
					return StringUtils.ToHexLiteral(data);

				case eDebugMode.MixedAsciiHex:
					return StringUtils.ToMixedReadableHexLiteral(data);

				case eDebugMode.Xml:
					return XmlUtils.Format(data);

				case eDebugMode.Json:
					return JsonUtils.Format(data);

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Gets the human readable string for the given mode.
		/// </summary>
		/// <param name="mode"></param>
		/// <returns></returns>
		private static string GetModeString(eDebugMode mode)
		{
			switch (mode)
			{
				case eDebugMode.Off:
				case eDebugMode.Ascii:
				case eDebugMode.Hex:
				case eDebugMode.Xml:
				case eDebugMode.Json:
					return mode.ToString().ToUpper();
					
				case eDebugMode.MixedAsciiHex:
					return "Mixed";

				default:
					throw new ArgumentOutOfRangeException("mode");
			}
		}
	}
}
