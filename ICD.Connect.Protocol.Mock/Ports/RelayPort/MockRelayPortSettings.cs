using ICD.Connect.Protocol.Ports.RelayPort;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.Mock.Ports.RelayPort
{
	[KrangSettings("MockRelayPort", typeof(MockRelayPort))]
	public sealed class MockRelayPortSettings : AbstractRelayPortSettings
	{
	}
}
