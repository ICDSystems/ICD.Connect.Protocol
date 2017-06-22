using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.Crosspoints.CrosspointManagers
{
	public delegate void CrosspointManagerCrosspointCallback(ICrosspointManager sender, ICrosspoint crosspoint);

	public interface ICrosspointManager
	{
		/// <summary>
		/// Raised when a crosspoint is registered with the manager.
		/// </summary>
		[PublicAPI]
		event CrosspointManagerCrosspointCallback OnCrosspointRegistered;

		/// <summary>
		/// Raised when a crosspoint is unregistered from the manager.
		/// </summary>
		[PublicAPI]
		event CrosspointManagerCrosspointCallback OnCrosspointUnregistered;

		/// <summary>
		/// Gets the address of the crosspoint manager.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		HostInfo GetHostInfo();

		/// <summary>
		/// Gets the available crosspoints.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		IEnumerable<ICrosspoint> GetCrosspoints();
	}
}
