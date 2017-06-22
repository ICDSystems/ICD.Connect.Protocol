using System;
using ICD.Common.EventArguments;
using ICD.Common.Properties;

namespace ICD.Connect.Protocol.Ports
{
	public interface IRelayPort : IPort
	{
		/// <summary>
		/// Raises when the relay opens or closes.
		/// </summary>
		[PublicAPI]
		event EventHandler<BoolEventArgs> OnClosedStateChanged;

		/// <summary>
		/// Get the state of the relay.
		/// </summary>
		[PublicAPI]
		bool Closed { get; }

		/// <summary>
		/// Open the relay
		/// </summary>
		[PublicAPI]
		void Open();

		/// <summary>
		/// Close the relay
		/// </summary>
		[PublicAPI]
		void Close();
	}
}
