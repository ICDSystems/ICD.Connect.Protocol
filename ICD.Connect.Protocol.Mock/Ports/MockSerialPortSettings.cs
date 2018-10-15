using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.Mock.Ports
{
	[KrangSettings("MockSerialPort", typeof(MockSerialPort))]
	public sealed class MockSerialPortSettings : AbstractSerialPortSettings
	{
	}
}