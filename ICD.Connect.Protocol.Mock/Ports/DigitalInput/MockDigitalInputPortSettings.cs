using ICD.Connect.Protocol.Ports.DigitalInput;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.Mock.Ports.DigitalInput
{
	[KrangSettings("MockDigitalInputPort", typeof(MockDigitalInputPort))]
	public sealed class MockDigitalInputPortSettings : AbstractDigitalInputPortSettings
	{
	}
}
