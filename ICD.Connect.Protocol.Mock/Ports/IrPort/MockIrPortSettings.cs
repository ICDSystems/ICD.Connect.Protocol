using ICD.Connect.Protocol.Ports.IrPort;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.Mock.Ports.IrPort
{
	[KrangSettings("MockIrPort", typeof(MockIrPort))]
	public sealed class MockIrPortSettings : AbstractIrPortSettings
	{
	}
}
