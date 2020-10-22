using ICD.Connect.Protocol.Network.Ports.Mqtt;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.NetworkPro.Ports.Mqtt
{
	[KrangSettings("MQTT", typeof(IcdMqttClient))]
	public sealed class IcdMqttClientSettings : AbstractMqttClientSettings
	{
	}
}
