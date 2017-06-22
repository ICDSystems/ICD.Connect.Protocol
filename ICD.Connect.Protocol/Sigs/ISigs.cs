namespace ICD.Connect.Protocol.Sigs
{
	public interface IBoolInputSig : ISig
	{
		bool SetBoolValue(bool value);
	}

	public interface IBoolOutputSig : ISig
	{
	}

	public interface IStringInputSig : ISig
	{
		bool SetStringValue(string value);
	}

	public interface IStringOutputSig : ISig
	{
	}

	public interface IUShortInputSig : ISig
	{
		bool SetUShortValue(ushort value);
	}

	public interface IUShortOutputSig : ISig
	{
	}
}
