using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.Network.Ports
{
	public interface INetworkPort : ISerialPort
	{
		/// <summary>
		/// Gets/sets the hostname of the remote server.
		/// </summary>
		string Address { get; set; }

		/// <summary>
		/// Gets/sets the port of the remote server.
		/// </summary>
		ushort Port { get; set; }

		/// <summary>
		/// Applies the given device configuration properties to the port.
		/// </summary>
		/// <param name="properties"></param>
		void ApplyDeviceConfiguration(INetworkProperties properties);

		/// <summary>
		/// Applies the given configuration properties to the port.
		/// </summary>
		/// <param name="properties"></param>
		void ApplyConfiguration(INetworkProperties properties);
	}
}
