using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
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
	}
}