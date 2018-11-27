using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.Mock.Ports.ComPort
{
	[KrangSettings("MockComPort", typeof(MockComPort))]
	public sealed class MockComPortSettings : AbstractComPortSettings
	{
	}
}