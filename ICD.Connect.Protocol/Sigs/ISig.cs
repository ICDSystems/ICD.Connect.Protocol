using System;
using ICD.Common.Properties;

namespace ICD.Connect.Protocol.Sigs
{
	public enum eSigType
	{
		[PublicAPI] Na,
		[PublicAPI] Digital,
		[PublicAPI] Analog,
		[PublicAPI] Serial
	}

	[PublicAPI]
	public enum eSigIoMask
	{
		[PublicAPI] Na,
		[PublicAPI] OutputSigOnly,
		[PublicAPI] InputSigOnly,
		[PublicAPI] InputOutputSig,
	}

	public interface ISig
	{
		/// <summary>
		/// Type of data this sig uses when communicating with the device.
		/// </summary>
		eSigType Type { get; }

		/// <summary>
		/// Number of this sig.
		/// </summary>
		uint Number { get; }

		/// <summary>
		/// Name of this Sig.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Get the string representation of this Sig.
		/// </summary>
		/// <exception cref="T:System.InvalidOperationException">Sig is in an invalid state.</exception>
		string GetStringValue();

		/// <summary>
		/// Get the UShort representation of this Sig.
		/// </summary>
		/// <exception cref="T:System.InvalidOperationException">Sig is in an invalid state.</exception>
		ushort GetUShortValue();

		/// <summary>
		/// Get the bool representation of this Sig.
		/// </summary>
		/// <exception cref="T:System.InvalidOperationException">Sig is in an invalid state.</exception>
		bool GetBoolValue();
	}

	/// <summary>
	/// Extension methods for working with ISigs.
	/// </summary>
	public static class SigExtensions
	{
		/// <summary>
		/// Returns the wrapped value (e.g. string data for a serial sig).
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static object GetValue(this ISig extends)
		{
			switch (extends.Type)
			{
				case eSigType.Digital:
					return extends.GetBoolValue();
				case eSigType.Analog:
					return extends.GetUShortValue();
				case eSigType.Serial:
					return extends.GetStringValue();

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Returns true if the sig has a value assigned to it.
		/// I.e. digital is true, analog is not 0, serial is not null.
		/// </summary>
		/// <returns></returns>
		public static bool HasValue(this ISig extends)
		{
			switch (extends.Type)
			{
				case eSigType.Na:
					return false;
				case eSigType.Digital:
					return extends.GetBoolValue();
				case eSigType.Analog:
					return extends.GetUShortValue() > 0;
				case eSigType.Serial:
					return extends.GetStringValue() != null;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}
