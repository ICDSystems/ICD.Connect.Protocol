using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;

namespace ICD.Connect.Protocol.Network.RemoteProcedure
{
	/// <summary>
	/// The ClientSerialRpcController simplifies using RPCs over a serial connection.
	/// </summary>
	[PublicAPI]
	public sealed class ClientSerialRpcController : IDisposable
	{
		private readonly ISerialBuffer m_Buffer;
		private readonly object m_Parent;

		private readonly ConnectionStateManager m_ConnectionStateManager;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		public ClientSerialRpcController(object parent)
		{
			m_Buffer = new JsonSerialBuffer();
			m_Parent = parent;

			m_ConnectionStateManager = new ConnectionStateManager(this){ConfigurePort = ConfigurePort};
			m_ConnectionStateManager.OnSerialDataReceived += PortOnSerialDataReceived;

			Subscribe(m_Buffer);
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			Unsubscribe(m_Buffer);

			m_ConnectionStateManager.OnSerialDataReceived -= PortOnSerialDataReceived;
			m_ConnectionStateManager.Dispose();
		}

		/// <summary>
		/// Calls the method on the server.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="parameters"></param>
		[PublicAPI]
		public void CallMethod(string key, params object[] parameters)
		{
			SendData(Rpc.CallMethodRpc(key, parameters));
		}

		/// <summary>
		/// Sets the property on the server.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		[PublicAPI]
		public void SetProperty(string key, object value)
		{
			SendData(Rpc.SetPropertyRpc(key, value));
		}

		/// <summary>
		/// Sends the serial data to the port.
		/// </summary>
		/// <param name="data"></param>
		private void SendData(ISerialData data)
		{
			if (!m_ConnectionStateManager.IsConnected)
			{
				ServiceProvider.TryGetService<ILoggerService>()
				               .AddEntry(eSeverity.Error, "{0} - Unable to communicate with port - connection state is false", GetType().Name);
				return;
			}

			m_ConnectionStateManager.Send(data.Serialize());
		}

		/// <summary>
		/// Sets the port for communication with the remote RPC controller.
		/// </summary>
		/// <param name="port"></param>
		[PublicAPI]
		public void SetPort(ISerialPort port)
		{
			m_ConnectionStateManager.SetPort(port);
		}

		private void ConfigurePort(ISerialPort port)
		{
			m_Buffer.Clear();
		}

		#endregion

		#region Port Callbacks

		/// <summary>
		/// Called when the port receieves some serial data.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void PortOnSerialDataReceived(object sender, StringEventArgs args)
		{
			m_Buffer.Enqueue(args.Data);
		}

		#endregion

		#region Buffer Callbacks

		/// <summary>
		/// Subscribe to the buffer events.
		/// </summary>
		/// <param name="buffer"></param>
		private void Subscribe(ISerialBuffer buffer)
		{
			buffer.OnCompletedSerial += BufferOnCompletedSerial;
		}

		/// <summary>
		/// Unsubscribe from the buffer events.
		/// </summary>
		/// <param name="buffer"></param>
		private void Unsubscribe(ISerialBuffer buffer)
		{
			buffer.OnCompletedSerial -= BufferOnCompletedSerial;
		}

		/// <summary>
		/// Called when we get a complete JSON string from the port.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void BufferOnCompletedSerial(object sender, StringEventArgs args)
		{
			Rpc rpc = Rpc.Deserialize(args.Data);
			rpc.Execute(m_Parent);
		}

		#endregion
	}
}
