using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.Crosspoints.CrosspointManagers
{
	public delegate void CrosspointManagerCrosspointCallback(ICrosspointManager sender, ICrosspoint crosspoint);

	public interface ICrosspointManager : IConsoleNode, IDisposable
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

		int SystemId { get; }

		/// <summary>
		/// Gets the available crosspoints.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		IEnumerable<ICrosspoint> GetCrosspoints();

		/// <summary>
		/// Creates a default crosspoint and adds it to the manager.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		ICrosspoint CreateCrosspoint(int id, string name);

		void RegisterCrosspoint(ICrosspoint crosspoint);
		void UnregisterCrosspoint(ICrosspoint crosspoint);
	}

	public interface ICrosspointManager<T> : ICrosspointManager
		where T : ICrosspoint
	{
		/// <summary>
		/// Creates a default crosspoint and adds it to the manager.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		new T CreateCrosspoint(int id, string name);

		void RegisterCrosspoint(T crosspoint);
		void UnregisterCrosspoint(T crosspoint);
	}
}
