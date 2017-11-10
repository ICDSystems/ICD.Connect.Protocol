using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Protocol.XSig
{
	public static class XSigParser
	{
		public static IXSig Parse(string data)
		{
			byte[] bytes = StringUtils.ToBytes(data);

			if (DigitalXSig.IsDigital(bytes))
				return new DigitalXSig(bytes);

			if (AnalogXSig.IsAnalog(bytes))
				return new AnalogXSig(bytes);

			if (SerialXSig.IsSerial(bytes))
				return new SerialXSig(bytes);

			throw new FormatException(string.Format("{0} is not a valid XSig", StringUtils.ToHexLiteral(data)));
		}

		public static IEnumerable<IXSig> ParseMultiple(string data)
		{
			List<byte> bytes = StringUtils.ToBytes(data).ToList();

			while (bytes.Any())
			{
				if (DigitalXSig.IsDigital(bytes.Take(2)))
				{
					yield return new DigitalXSig(bytes.Take(2));
					bytes = bytes.Skip(2).ToList();
				}
				else if (AnalogXSig.IsAnalog(bytes.Take(4)))
				{
					yield return new AnalogXSig(bytes.Take(4));
					bytes = bytes.Skip(4).ToList();
				}
				else
				{
					var index = bytes.IndexOf(0xFF);
					if (SerialXSig.IsSerial(bytes.Take(index + 1)))
					{
						yield return new SerialXSig(bytes.Take(index + 1));
						bytes = bytes.Skip(index + 1).ToList();
					}
					else
						break;
				}
			}
		}

		public static bool IsValidDigitalSigHeader(byte[] bytes)
	    {
	        return bytes[0].GetBit(7) &&
	               !bytes[0].GetBit(6) &&
	               !bytes[1].GetBit(7);
	    }

	    public static bool IsValidAnalogSigHeader(byte[] bytes)
	    {
	        return bytes[0].GetBit(7) &&
	               bytes[0].GetBit(6) &&
	               !bytes[1].GetBit(7) &&
	               !bytes[2].GetBit(7) &&
	               !bytes[3].GetBit(7);
	    }

	    public static bool IsValidSerialSigHeader(byte[] bytes)
	    {
	        return bytes[0].GetBit(7) &&
	               bytes[0].GetBit(6) &&
	               !bytes[0].GetBit(5) &&
	               !bytes[0].GetBit(4) &&
	               bytes[0].GetBit(3) &&
	               !bytes[1].GetBit(0);
	    }

	    public static bool IsValidSerialSigTerminator(byte byteToCheck)
	    {
	        return !byteToCheck.GetBit(7) &&
	               !byteToCheck.GetBit(6) &&
	               !byteToCheck.GetBit(5) &&
	               !byteToCheck.GetBit(4) &&
	               !byteToCheck.GetBit(3) &&
	               !byteToCheck.GetBit(2) &&
	               !byteToCheck.GetBit(1) &&
	               !byteToCheck.GetBit(0);

	    }
	}
}