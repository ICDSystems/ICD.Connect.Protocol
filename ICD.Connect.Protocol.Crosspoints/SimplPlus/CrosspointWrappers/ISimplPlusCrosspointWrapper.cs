using System;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;

namespace ICD.Connect.Protocol.Crosspoints.SimplPlus.CrosspointWrappers
{
	public interface ISimplPlusCrosspointWrapper
	{

		#region Properties

		int SystemId { get; }

		int CrosspointId { get; }

		string CrosspointName { get; }

		string CrosspointSymbolInstanceName { get; }

		ICrosspoint Crosspoint { get; }

		#endregion

		#region Events

		event EventHandler OnCrosspointChanged;

		#endregion

		#region Methods

		string GetWrapperInfo();

		#endregion
	}
}