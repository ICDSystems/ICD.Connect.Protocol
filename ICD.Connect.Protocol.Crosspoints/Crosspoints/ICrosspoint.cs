using System;
using ICD.Common.Properties;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Crosspoints.EventArguments;

namespace ICD.Connect.Protocol.Crosspoints.Crosspoints
{
	public delegate void CrosspointDataReceived(ICrosspoint sender, CrosspointData data);

	/// <summary>
	/// Common interface for Control and Equipment crosspoints.
	/// </summary>
	public interface ICrosspoint : IDisposable, IConsoleNode
	{
		/// <summary>
		/// Raised when this crosspoint sends data to XP3.
		/// </summary>
		[PublicAPI]
		event CrosspointDataReceived OnSendInputData;

		/// <summary>
		/// Raised when XP3 sends data to this crosspoint.
		/// </summary>
		[PublicAPI]
		event CrosspointDataReceived OnSendOutputData;

		/// <summary>
		/// The Id of this crosspoint.
		/// </summary>
		[PublicAPI]
		int Id { get; }

		/// <summary>
		/// The human readable name of this crosspoint.
		/// </summary>
		[PublicAPI]
		string Name { get; }

		/// <summary>
		/// Called by XP3 to send data to this crosspoint.
		/// </summary>
		/// <param name="data"></param>
		[PublicAPI]
		void SendOutputData(CrosspointData data);

		/// <summary>
		/// Called by the program to send data to XP3.
		/// </summary>
		/// <param name="data"></param>
		[PublicAPI]
		void SendInputData(CrosspointData data);

		/// <summary>
		/// Gets or sets the status of the crosspoint
		/// </summary>
		[PublicAPI]
		eCrosspointStatus Status { get; }

		/// <summary>
		/// Raised when the status of this crosspoint changes.
		/// </summary>
		[PublicAPI]
		event EventHandler<CrosspointStatusEventArgs> OnStatusChanged;
	}
}
