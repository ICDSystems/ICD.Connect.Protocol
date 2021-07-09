using ICD.Connect.Devices;

namespace ICD.Connect.Protocol.Network.Devices.PortServers
{
	public interface IPortServerDeviceSettings : IDeviceSettings
	{
		int? Port { get; set; }
		ushort? TcpServerPort { get; set; }
	}
}