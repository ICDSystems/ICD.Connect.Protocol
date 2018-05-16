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
	}
}
