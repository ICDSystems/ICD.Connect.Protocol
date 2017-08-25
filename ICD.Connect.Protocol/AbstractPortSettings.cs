using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Settings
{
	/// <summary>
	/// Base class for port settings.
	/// </summary>
	public abstract class AbstractPortSettings : AbstractDeviceBaseSettings, IPortSettings
	{
		public const string PORT_ELEMENT = "Port";

		/// <summary>
		/// Gets the xml element.
		/// </summary>
		protected override string Element { get { return PORT_ELEMENT; } }

		/// <summary>
		/// Parses the xml and applies the properties to the instance.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="xml"></param>
		protected static void ParseXml(AbstractPortSettings instance, string xml)
		{
			AbstractDeviceBaseSettings.ParseXml(instance, xml);
		}
	}
}
