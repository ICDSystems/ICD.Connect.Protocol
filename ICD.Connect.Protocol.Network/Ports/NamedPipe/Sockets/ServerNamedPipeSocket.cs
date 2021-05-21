#if !SIMPLSHARP
using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using ICD.Common.Properties;

namespace ICD.Connect.Protocol.Network.Ports.NamedPipe.Sockets
{
	public sealed class ServerNamedPipeSocket : AbstractNamedPipeSocket<NamedPipeServerStream>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="pipeName"></param>
		public ServerNamedPipeSocket([NotNull] NamedPipeServerStream stream, string pipeName)
			: base(stream, pipeName)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");
		}

		/// <summary>
		/// Connects the stream.
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="token"></param>
		protected override Task ConnectAsync([NotNull] NamedPipeServerStream stream, CancellationToken token)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");

			return stream.WaitForConnectionAsync(token);
		}
	}
}
#endif
