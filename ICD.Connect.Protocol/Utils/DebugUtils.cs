using System;
using ICD.Common.Utils;
using ICD.Common.Utils.Json;
using ICD.Common.Utils.Xml;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.Utils
{
	public static class DebugUtils
	{
		private const string RX = "RX";
		private const string TX = "TX";

		private static readonly SafeCriticalSection s_LoggingSection;

		/// <summary>
		/// Static constructor.
		/// </summary>
		static DebugUtils()
		{
			s_LoggingSection = new SafeCriticalSection();
		}

		/// <summary>
		/// Formats and prints the received data to the console.
		/// Does nothing if mode is off.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="mode"></param>
		/// <param name="data"></param>
		public static void PrintRx(object instance, eDebugMode mode, object data)
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
		public static void PrintRx(object instance, eDebugMode mode, string context, object data)
		{
			PrintData(instance, context, data, RX, eConsoleColor.Red, mode);
		}

		/// <summary>
		/// Formats and prints the transmitted data to the console.
		/// Does nothing if mode is off.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="mode"></param>
		/// <param name="data"></param>
		public static void PrintTx(object instance, eDebugMode mode, object data)
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
		public static void PrintTx(object instance, eDebugMode mode, string context, object data)
		{
			PrintData(instance, context, data, TX, eConsoleColor.Green, mode);
		}

		/// <summary>
		/// Formats and prints the data to the console.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="context"></param>
		/// <param name="data"></param>
		/// <param name="direction"></param>
		/// <param name="directionColor"></param>
		/// <param name="mode"></param>
		public static void PrintData(object instance, string context, object data, string direction, eConsoleColor directionColor, eDebugMode mode)
		{
			if (mode == eDebugMode.Off)
				return;

			string modeString = GetModeString(mode);

			// Massage the data
			data = FormatData(data, mode);

			s_LoggingSection.Enter();

			try
			{
				// "[App X] Instance Context - Direction(Mode) - Data"
				// "[App 1] Port(Id=1) ClientId:10 - TX(Ascii) - SomeData"
				IcdConsole.Print("[App {0}] {1}", ProgramUtils.ProgramNumber, instance);

				if (!string.IsNullOrEmpty(context))
					IcdConsole.Print(" {0}", context);
				IcdConsole.Print(" - ");

				IcdConsole.Print(directionColor, direction);
				IcdConsole.Print("({0}) - {1}", modeString, data);
				IcdConsole.PrintLine(string.Empty);
			}
			finally
			{
				s_LoggingSection.Leave();
			}
		}

		/// <summary>
		/// Formats the data based on the given debug mode.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="mode"></param>
		/// <returns></returns>
		private static string FormatData(object data, eDebugMode mode)
		{
			if (data == null)
				return null;

			switch (mode)
			{
				case eDebugMode.Off:
					return null;

				case eDebugMode.Ascii:
					string dataString = data.ToString();
					dataString = dataString.Replace("\n", "\\n");
					dataString = dataString.Replace("\r", "\\r");
					return dataString;

				case eDebugMode.Hex:
					return StringUtils.ToHexLiteral(data.ToString());

				case eDebugMode.MixedAsciiHex:
					return StringUtils.ToMixedReadableHexLiteral(data.ToString());

				case eDebugMode.Xml:
					try
					{
						return XmlUtils.Format(data.ToString());
					}
					catch (IcdXmlException)
					{
						return FormatData("(Invalid XML)" + data, eDebugMode.Ascii);
					}

				case eDebugMode.Json:
					try
					{
						return JsonUtils.Format(data);
					}
					catch (Exception)
					{
						return FormatData("(Invalid JSON)" + data, eDebugMode.Ascii);
					}

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
