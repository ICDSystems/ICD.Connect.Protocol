namespace ICD.Connect.Settings
{
	/// <summary>
	/// Base class for port settings.
	/// </summary>
	public abstract class AbstractPortSettings : AbstractDeviceSettings
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
			AbstractDeviceSettings.ParseXml(instance, xml);
		}
	}
}
