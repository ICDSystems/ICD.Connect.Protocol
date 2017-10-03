using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Protocol.Ports.RelayPort
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
