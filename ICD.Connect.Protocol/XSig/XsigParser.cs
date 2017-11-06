using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.Sigs;

namespace ICD.Connect.Protocol.XSig
{
	public static class Xsig
	{

		public static IEnumerable<SigInfo> ParseMultiple(string xsigString)
		{
			List<byte> bytes = StringUtils.ToBytes(xsigString).ToList();
			bool noSigFound = false;
			while(bytes.Any() && !noSigFound)
			{
				if (DigitalXsig.IsDigital(bytes.Take(2)))
				{
					var xsig = new DigitalXsig(bytes.Take(2));
					yield return new SigInfo((ushort)(xsig.Index + 1), 0, xsig.Value);
					bytes = bytes.Skip(2).ToList();
				}
				else if (AnalogXsig.IsAnalog(bytes.Take(4)))
				{
					var xsig = new AnalogXsig(bytes.Take(4));
					yield return new SigInfo((ushort)(xsig.Index + 1), 0, xsig.Value);
					bytes = bytes.Skip(4).ToList();
				}
				else
				{
					var index = bytes.IndexOf(0xFF);
					if (SerialXsig.IsSerial(bytes.Take(index + 1)))
					{
						var xsig = new SerialXsig(bytes.Take(index + 1));
						yield return new SigInfo((ushort)(xsig.Index+1), 0, xsig.Value);
						bytes = bytes.Skip(index + 1).ToList();
					}
					else
						noSigFound = true;
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