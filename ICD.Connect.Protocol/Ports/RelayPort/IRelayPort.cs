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

	public static class RelayPortExtensions
	{
		/// <summary>
		/// Sets the closed state of the relay port.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="closed"></param>
		public static void SetClosed([NotNull] this IRelayPort extends, bool closed)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			extends.SetOpen(!closed);
		}

		/// <summary>
		/// Sets the open state of the relay port.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="open"></param>
		public static void SetOpen([NotNull] this IRelayPort extends, bool open)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (open)
				extends.Open();
			else
				extends.Close();
		}
	}
}
