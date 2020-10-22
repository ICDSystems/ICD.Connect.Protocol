using System;

namespace ICD.Connect.Protocol.Network.EventArguments
{
	public sealed class MqttMessageEventArgs : EventArgs
	{
		private readonly string m_Topic;
		private readonly byte[] m_Message;

		/// <summary>
		/// Gets the topic.
		/// </summary>
		public string Topic { get { return m_Topic; } }

		/// <summary>
		/// Gets the message.
		/// </summary>
		public byte[] Message { get { return m_Message; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="topic"></param>
		/// <param name="message"></param>
		public MqttMessageEventArgs(string topic, byte[] message)
		{
			m_Topic = topic;
			m_Message = message;
		}
	}
}
