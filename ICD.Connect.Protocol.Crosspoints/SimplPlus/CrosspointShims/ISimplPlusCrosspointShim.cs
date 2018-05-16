using ICD.Common.Properties;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using ICD.Connect.Settings.SPlusShims;

namespace ICD.Connect.Protocol.Crosspoints.SimplPlus.CrosspointShims
{
	public delegate void SPlusJoinXSigCallback(string xsig);

	public delegate void SPlusStatusUpdateCallback(ushort status);

	public delegate void SPlusCrosspointChangedCallback();

	public interface ISimplPlusCrosspointShim : ISPlusShim
	{
		[PublicAPI("S+")]
		int SystemId { get; }

		[PublicAPI("S+")]
		int CrosspointId { get; }

		[PublicAPI("S+")]
		string CrosspointName { get; }

		[PublicAPI("S+")]
		string CrosspointSymbolInstanceName { get; }

		ICrosspoint Crosspoint { get; }

		string GetShimInfo();
	}

	public interface ISimplPlusCrosspointShim<T> : ISimplPlusCrosspointShim
	{
		new T Crosspoint { get; }
	}
}
