using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.NetworkPro.Ports
{
	[KrangSettings("MockMqttClient", typeof(MockMqttClient))]
	public sealed class MockMqttClientSettings : AbstractMqttClientSettings
	{
	}
}
