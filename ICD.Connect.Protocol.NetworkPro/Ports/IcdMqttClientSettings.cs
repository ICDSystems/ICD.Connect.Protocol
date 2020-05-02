using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.NetworkPro.Ports
{
	[KrangSettings("MQTT", typeof(IcdMqttClient))]
	public sealed class IcdMqttClientSettings : AbstractMqttClientSettings
	{
	}
}
