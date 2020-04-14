using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.IoT.Ports
{
	[KrangSettings("MQTT", typeof(IcdMqttClient))]
	public sealed class IcdMqttClientSettings : AbstractMqttClientSettings
	{
	}
}
