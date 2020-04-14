using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.IoT.Ports
{
	[KrangSettings("MockMqttClient", typeof(MockMqttClient))]
	public sealed class MockMqttClientSettings : AbstractMqttClientSettings
	{
	}
}
