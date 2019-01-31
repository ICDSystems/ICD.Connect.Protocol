using System;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.Network.Direct
{
	public interface IMessage : ISerialData
	{
		HostInfo MessageTo { get; set; }
		HostInfo MessageFrom { get; set; }
		Guid MessageId { get; set; }
		string Type { get; }
	}
}