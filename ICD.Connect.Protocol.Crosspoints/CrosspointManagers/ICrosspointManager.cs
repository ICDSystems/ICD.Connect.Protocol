using System;
using System.Collections.Generic;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.Crosspoints.CrosspointManagers
{
	public delegate void CrosspointManagerCrosspointCallback(ICrosspointManager sender, ICrosspoint crosspoint);

	public interface ICrosspointManager : IConsoleNode, IDisposable
	{
		event CrosspointManagerCrosspointCallback OnCrosspointRegistered;
		event CrosspointManagerCrosspointCallback OnCrosspointUnregistered;

		int SystemId { get; }

		HostInfo GetHostInfo();
		IEnumerable<ICrosspoint> GetCrosspoints();

		void RegisterCrosspoint(ICrosspoint crosspoint);
		void UnregisterCrosspoint(ICrosspoint crosspoint);
	}

	public interface ICrosspointManager<T> : ICrosspointManager
		where T : ICrosspoint
	{
		void RegisterCrosspoint(T crosspoint);
		void UnregisterCrosspoint(T crosspoint);
	}
}
