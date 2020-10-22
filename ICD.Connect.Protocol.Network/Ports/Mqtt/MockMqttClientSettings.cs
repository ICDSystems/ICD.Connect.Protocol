using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.Network.Ports.Mqtt
{
	[KrangSettings("MockMqttClient", typeof(MockMqttClient))]
	public sealed class MockMqttClientSettings : AbstractMqttClientSettings
	{
	}
}
