using System;
using ICD.Common.EventArguments;

namespace ICD.Connect.Protocol.Ports
{
	public enum eIoPortConfiguration
	{
		None,
		DigitalIn,
		DigitalOut,
		AnalogIn
	}

	public delegate void IoPortConfigurationCallback(IIoPort port, eIoPortConfiguration configuration);

	public interface IIoPort : IPort
	{
		/// <summary>
		/// Raised when a change in the digital input signal is detected.
		/// </summary>
		event EventHandler<BoolEventArgs> OnDigitalInChanged;

		/// <summary>
		/// Raised when a change in the digital output signal is detected.
		/// </summary>
		event EventHandler<BoolEventArgs> OnDigitalOutChanged;

		/// <summary>
		/// Raised when a change in the analog input signal is detected.
		/// </summary>
		event EventHandler<UShortEventArgs> OnAnalogInChanged;

		/// <summary>
		/// Raised when the IO configuration changes.
		/// </summary>
		event IoPortConfigurationCallback OnConfigurationChanged;

		/// <summary>
		/// Gets the current digital input state.
		/// </summary>
		bool DigitalIn { get; }

		/// <summary>
		/// Gets the current digital output state.
		/// </summary>
		bool DigitalOut { get; }

		/// <summary>
		/// Gets the current analog input state.
		/// </summary>
		ushort AnalogIn { get; }

		/// <summary>
		/// Gets the current configuration mode.
		/// </summary>
		eIoPortConfiguration Configuration { get; }

		/// <summary>
		/// Sets the configuration mode.
		/// </summary>
		void SetConfiguration(eIoPortConfiguration configuration);

		/// <summary>
		/// Sets the digital output state.
		/// </summary>
		/// <param name="digitalOut"></param>
		void SetDigitalOut(bool digitalOut);
	}
}
