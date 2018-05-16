using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.Network.Ports
{
	public abstract class AbstractNetworkPort<TSettings> : AbstractSerialPort<TSettings>, INetworkPort
		where TSettings : INetworkPortSettings, new()
	{
		/// <summary>
		/// Gets/sets the hostname of the remote server.
		/// </summary>
		public abstract string Address { get; set; }

		/// <summary>
		/// Gets/sets the port of the remote server.
		/// </summary>
		public abstract ushort Port { get; set; }
	}
}
