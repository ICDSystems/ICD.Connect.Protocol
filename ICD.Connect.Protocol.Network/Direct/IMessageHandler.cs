namespace ICD.Connect.Protocol.Network.Direct
{
	internal interface IMessageHandler
	{
		AbstractMessage HandleMessage(object message);
	}
}
