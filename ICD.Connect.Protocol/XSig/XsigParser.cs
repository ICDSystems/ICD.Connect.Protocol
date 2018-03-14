using System;
using ICD.Common.Utils;

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
	}
}
