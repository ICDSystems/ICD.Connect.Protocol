using System;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.Network.Direct
{
	public interface IMessage : ISerialData
	{
		uint ClientId { get; set; }
		HostInfo MessageFrom { get; set; }
		Guid MessageId { get; set; }
		string Type { get; }
	}
}