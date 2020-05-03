using System;
using ICD.Common.Utils.Extensions;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace ICD.Connect.Protocol.NetworkPro.Ports.WebSockets
{
	public sealed class CallbackWebSocketBehavior : WebSocketBehavior
	{
		public event EventHandler<CloseEventArgs> OnClosed;
		public event EventHandler<ErrorEventArgs> OnErrored;
		public event EventHandler<MessageEventArgs> OnMessageReceived;
		public event EventHandler OnOpened; 

		protected override void OnClose(CloseEventArgs e)
		{
			base.OnClose(e);

			OnClosed.Raise(this, e);
		}

		protected override void OnError(ErrorEventArgs e)
		{
			base.OnError(e);

			OnErrored.Raise(this, e);
		}

		protected override void OnMessage(MessageEventArgs e)
		{
			base.OnMessage(e);

			OnMessageReceived.Raise(this, e);
		}

		protected override void OnOpen()
		{
			base.OnOpen();

			OnOpened.Raise(this);
		}
	}
}
