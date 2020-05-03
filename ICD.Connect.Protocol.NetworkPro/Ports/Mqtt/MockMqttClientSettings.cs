using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.NetworkPro.Ports.Mqtt
{
	[KrangSettings("MockMqttClient", typeof(MockMqttClient))]
	public sealed class MockMqttClientSettings : AbstractMqttClientSettings
	{
	}
}
