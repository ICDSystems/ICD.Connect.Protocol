#if !SIMPLSHARP
using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Protocol.Network.Ports.NamedPipe.Sockets
{
	public abstract class AbstractNamedPipeSocket<TStream> : IDisposable
		where TStream : PipeStream
	{
		private const int DEFAULT_BUFFER_SIZE = 16384;

		/// <summary>
		/// Raised when the socket connects/disconnects.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnIsConnectedChanged;

		/// <summary>
		/// Raised when data is received from the far end.
		/// </summary>
		public event EventHandler<GenericEventArgs<byte[]>> OnDataReceived; 

		private readonly TStream m_Stream;
		private readonly string m_PipeName;
		private readonly CancellationTokenSource m_Cancellation;
		private readonly byte[] m_Buffer;

		private bool m_IsConnected;

		#region Properties

		/// <summary>
		/// Gets the underlying stream.
		/// </summary>
		public TStream Stream { get { return m_Stream; } }

		/// <summary>
		/// Gets the pipe name.
		/// </summary>
		public string PipeName { get { return m_PipeName; } }

		/// <summary>
		/// Gets the connected state of the socket.
		/// </summary>
		public bool IsConnected
		{
			get { return m_IsConnected; }
			private set
			{
				if (value == m_IsConnected)
					return;

				m_IsConnected = value;

				OnIsConnectedChanged.Raise(this, m_IsConnected);
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="pipeName"></param>
		protected AbstractNamedPipeSocket([NotNull] TStream stream, string pipeName)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");
			
			m_Cancellation = new CancellationTokenSource();
			m_Buffer = new byte[DEFAULT_BUFFER_SIZE];

			m_Stream = stream;
			m_PipeName = pipeName;
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			OnIsConnectedChanged = null;
			OnDataReceived = null;

			m_Cancellation.Cancel();
			m_Stream.Dispose();

			UpdateIsConnected();
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			ReprBuilder builder = new ReprBuilder(this);
			BuildStringRepresentationProperties((n, v) => builder.AppendProperty(n, v));
			return builder.ToString();
		}

		/// <summary>
		/// Connects to the end point.
		/// </summary>
		public Task ConnectAsync()
		{
			return ConnectAsync(m_Stream, m_Cancellation.Token).ContinueWith(HandleConnected, m_Cancellation.Token);
		}

		/// <summary>
		/// Sends the data to the remote endpoint.
		/// </summary>
		public Task SendAsync([NotNull] byte[] data)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));

			return m_Stream.WriteAsync(data, 0, data.Length, m_Cancellation.Token);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Connects the stream.
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="token"></param>
		protected abstract Task ConnectAsync([NotNull] TStream stream, CancellationToken token);

		/// <summary>
		/// Called when a connection is established.
		/// </summary>
		/// <param name="task"></param>
		private void HandleConnected(Task task)
		{
			UpdateIsConnected();
			Arm();
		}

		/// <summary>
		/// Re-arms the stream to read from the far end.
		/// </summary>
		private void Arm()
		{
			if (!m_Stream.IsConnected)
				return;

			m_Stream.ReadAsync(m_Buffer, 0, m_Buffer.Length, m_Cancellation.Token)
			        .ContinueWith(ReceiveHandler, m_Cancellation.Token);
		}

		/// <summary>
		/// Updates the IsConnected property to reflect the underlying stream.
		/// </summary>
		private void UpdateIsConnected()
		{
			IsConnected = m_Stream.IsConnected;
		}

		/// <summary>
		/// Override to add additional properties to the ToString representation.
		/// </summary>
		/// <param name="addPropertyAndValue"></param>
		protected virtual void BuildStringRepresentationProperties([NotNull] Action<string, object> addPropertyAndValue)
		{
			if (addPropertyAndValue == null)
				throw new ArgumentNullException("addPropertyAndValue");

			addPropertyAndValue("PipeName", PipeName);
		}

		/// <summary>
		/// Handles Receiving Data from the Active Named Pipe Connection
		/// </summary>
		/// <param name="task"></param>
		private void ReceiveHandler([NotNull] Task<int> task)
		{
			if (task == null)
				throw new ArgumentNullException("task");

			if (!task.IsFaulted && task.Result > 0)
			{
				int bytesRead = task.Result;
				byte[] data = new byte[bytesRead];
				Array.Copy(m_Buffer, 0, data, 0, bytesRead);

				OnDataReceived.Raise(this, data);
			}

			UpdateIsConnected();
			Arm();
		}

		#endregion
	}
}
#endif
