using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Protocol.Network.Tcp
{
	public static class TelnetControl
	{
		public const byte HEADER = 0xFF;

		public enum eCommand
		{
			Will = 0xFB,
			Do = 0xFD,
			Dont = 0XFE,
			Wont = 0xFC
		}

		/// <summary>
		/// Builds the negotiation serial for the given command and option.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="option"></param>
		/// <returns></returns>
		public static byte[] BuildNegotiation(eCommand command, byte option)
		{
			return new[] {HEADER, (byte)command, option};
		}

		/// <summary>
		/// Returns the rejection for the given negotiation serial.
		/// </summary>
		/// <param name="serial"></param>
		/// <returns></returns>
		public static string Reject(string serial)
		{
			IEnumerable<byte> bytes = Reject(StringUtils.ToBytes(serial));
			return StringUtils.ToString(bytes);
		}

		/// <summary>
		/// Returns the rejection for the given negotiation serial.
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns></returns>
		public static IEnumerable<byte> Reject(IEnumerable<byte> bytes)
		{
			foreach (byte[] triple in bytes.Partition(3).Select(p => p.ToArray()))
			{
				byte header = triple[0];

				if (header != HEADER)
				{
					string message = string.Format("First byte ({0}) is not a Telnet negotiation header {1}.",
					                               triple[0], HEADER);
					throw new FormatException(message);
				}

				yield return header;
				yield return Reject(triple[1]);
				yield return triple[2];
			}
		}

		/// <summary>
		/// Returns the rejection command for the given command.
		/// </summary>
		/// <param name="command"></param>
		/// <returns></returns>
		public static byte Reject(byte command)
		{
			return (byte)Reject((eCommand)command);
		}

		/// <summary>
		/// Returns the rejection command for the given command.
		/// </summary>
		/// <param name="command"></param>
		/// <returns></returns>
		public static eCommand Reject(eCommand command)
		{
			switch (command)
			{
				case eCommand.Dont:
				case eCommand.Wont:
					return command;

				case eCommand.Will:
					return eCommand.Dont;
				case eCommand.Do:
					return eCommand.Wont;

				default:
					throw new ArgumentOutOfRangeException("command");
			}
		}
	}
}
