﻿namespace ICD.Connect.Protocol.NetworkPro.Ports.Mqtt
{
	public sealed class LastWillAndTestament
	{
		public bool Retain { get; set; }
		public byte QosLevel { get; set; }
		public bool Flag { get; set; }
		public string Topic { get; set; }
		public string Message { get; set; }
	}
}