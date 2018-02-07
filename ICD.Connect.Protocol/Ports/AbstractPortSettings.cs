using ICD.Connect.Devices;

namespace ICD.Connect.Protocol.Ports
{
	/// <summary>
	/// Base class for port settings.
	/// </summary>
	public abstract class AbstractPortSettings : AbstractDeviceBaseSettings, IPortSettings
	{
		private const string PORT_ELEMENT = "Port";

		/// <summary>
		/// Gets the xml element.
		/// </summary>
		protected override string Element { get { return PORT_ELEMENT; } }
	}
}
