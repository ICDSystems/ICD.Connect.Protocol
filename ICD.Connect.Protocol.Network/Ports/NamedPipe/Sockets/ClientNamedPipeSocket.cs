#if !SIMPLSHARP
using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using ICD.Common.Properties;

namespace ICD.Connect.Protocol.Network.Ports.NamedPipe.Sockets
{
	public sealed class ClientNamedPipeSocket : AbstractNamedPipeSocket<NamedPipeClientStream>
	{
		private readonly string m_Hostname;

		/// <summary>
		/// Gets/sets the configurable remote hostname.
		/// </summary>
		public string Hostname { get { return m_Hostname; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="hostname"></param>
		/// <param name="pipeName"></param>
		public ClientNamedPipeSocket([NotNull] NamedPipeClientStream stream, string hostname, string pipeName)
			: base(stream, pipeName)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");

			m_Hostname = hostname;
		}

		/// <summary>
		/// Connects the stream.
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="token"></param>
		protected override Task ConnectAsync([NotNull] NamedPipeClientStream stream, CancellationToken token)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");

			return stream.ConnectAsync(token);
		}

		/// <summary>
		/// Override to add additional properties to the ToString representation.
		/// </summary>
		/// <param name="addPropertyAndValue"></param>
		protected override void BuildStringRepresentationProperties(Action<string, object> addPropertyAndValue)
		{
			addPropertyAndValue("Hostname", Hostname);

			base.BuildStringRepresentationProperties(addPropertyAndValue);
		}
	}
}
#endif
