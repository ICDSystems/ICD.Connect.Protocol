using System;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Connect.Protocol.Data;

namespace ICD.Connect.Protocol.Network.Ports.Tcp
{
	public struct TelnetCommand : IEquatable<TelnetCommand>, ISerialData
	{
		public const byte HEADER = 0xFF;

		public enum eCommand
		{
			Will = 0xFB,
			Do = 0xFD,
			Dont = 0xFE,
			Wont = 0xFC
		}

		public enum eOption
		{
			BinaryTransmit = 0x00,
			Echo = 0x01,
			Reconnect = 0x02,
			SuppressGoAhead = 0x03,
			MessageSize = 0x04,
			OptionStatus = 0x05,
			TimingMark = 0x06,
			RemoteControlTerminalPrinters = 0x07,
			LineWidth = 0x08,
			PageLength = 0x09,
			CarriageReturnUse = 0x0A,
			HorizontalTabs = 0x0B,
			HorizontalTabUse = 0X0C,
			FormFeedUse = 0x0D,
			VerticalTabs = 0X0E,
			VerticalTabUse = 0x0F,
			LineFeedUse = 0x10,
			ExtendedAscii = 0x11,
			Logout = 0x12,
			ByteMacro = 0x13,
			DataTerminal = 0x14,
			UseSupdup = 0x15,
			SupdupOutput = 0x16,
			SendLocate = 0x17,
			TerminalType = 0x18,
			EndRecord = 0x19,
			TacacsId = 0x1A,
			OutputMark = 0x1B,
			TerminalLocationId = 0x1C,
			Emulate3270Terminals = 0x1D,
			X3ProtocolEmulation = 0x1E,
			WindowSize = 0x1F,
			TermSpeed = 0x20,
			RemoteFlow = 0x21,
			LineMode = 0x22,

			Extended = 0xFF
		}

		private readonly eCommand m_Command;
		private readonly eOption m_Option;

		#region Properties

		/// <summary>
		/// Gets the command.
		/// </summary>
		public eCommand Command { get { return m_Command; } }

		/// <summary>
		/// Gets the option.
		/// </summary>
		public eOption Option { get { return m_Option; } }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public TelnetCommand(eCommand command, eOption option)
		{
			if (!EnumUtils.IsDefined(command))
				throw new FormatException("Invalid command");

			if (!EnumUtils.IsDefined(option))
				throw new FormatException("Invalid option");

			m_Command = command;
			m_Option = option;
		}

		/// <summary>
		/// Parses the given characters as a telnet command.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static TelnetCommand Parse([NotNull] string data)
		{
			if (data == null)
				throw new ArgumentNullException("data");

			if (data.Length != 3)
				throw new FormatException("String must be 3 characters long");

			if (data[0] != HEADER)
				throw new FormatException("Invalid header");

			eCommand command = (eCommand)data[1];
			eOption option = (eOption)data[2];

			return new TelnetCommand(command, option);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Returns the fully qualified type name of this instance.
		/// </summary>
		/// <returns>The fully qualified type name.</returns>
		public override string ToString()
		{
			return new ReprBuilder(this)
			       .AppendProperty("Command", m_Command)
			       .AppendProperty("Option", m_Option)
			       .ToString();
		}

		/// <summary>
		/// Serialize this instance to a string.
		/// </summary>
		/// <returns></returns>
		public string Serialize()
		{
			return StringUtils.ToString(new[] {HEADER, (byte)m_Command, (byte)m_Option});
		}

		/// <summary>
		/// Returns the rejection command for this command.
		/// </summary>
		/// <returns></returns>
		public TelnetCommand Reject()
		{
			eCommand reject = Reject(m_Command);
			return new TelnetCommand(reject, m_Option);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Returns the rejection command for the given command.
		/// </summary>
		/// <param name="command"></param>
		/// <returns></returns>
		private static eCommand Reject(eCommand command)
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

		#endregion

		#region Equality

		/// <summary>
		/// Implementing default equality.
		/// </summary>
		/// <param name="a1"></param>
		/// <param name="a2"></param>
		/// <returns></returns>
		public static bool operator ==(TelnetCommand a1, TelnetCommand a2)
		{
			return a1.Equals(a2);
		}

		/// <summary>
		/// Implementing default inequality.
		/// </summary>
		/// <param name="a1"></param>
		/// <param name="a2"></param>
		/// <returns></returns>
		public static bool operator !=(TelnetCommand a1, TelnetCommand a2)
		{
			return !a1.Equals(a2);
		}

		/// <summary>
		/// Returns true if this instance is equal to the given object.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool Equals(object other)
		{
			return other is TelnetCommand && Equals((TelnetCommand)other);
		}

		/// <summary>
		/// Returns true if this instance is equal to the given endpoint.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		[Pure]
		public bool Equals(TelnetCommand other)
		{
			return m_Command == other.m_Command &&
			       m_Option == other.m_Option;
		}

		/// <summary>
		/// Gets the hashcode for this instance.
		/// </summary>
		/// <returns></returns>
		[Pure]
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 23 + (byte)m_Command;
				hash = hash * 23 + (byte)m_Option;
				return hash;
			}
		}

		#endregion
	}
}
