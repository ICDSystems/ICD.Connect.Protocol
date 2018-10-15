using ICD.Connect.Protocol.Ports.IoPort;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.Mock.Ports.IoPort
{
	[KrangSettings("MockIoPort", typeof(MockIoPort))]
	public sealed class MockIoPortSettings : AbstractIoPortSettings
	{
	}
}
