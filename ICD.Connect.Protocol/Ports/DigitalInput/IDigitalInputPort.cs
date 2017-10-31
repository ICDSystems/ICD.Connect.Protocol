using System;
using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Protocol.Ports.DigitalInput
{

	public interface IDigitalInputPort : IPort
	{
		/// <summary>
		/// Raised when a change in the digital input signal is detected.
		/// </summary>
		event EventHandler<BoolEventArgs> OnStateChanged;

		/// <summary>
		/// Gets the current digital input state.
		/// </summary>
		bool State { get; }
	}
}
