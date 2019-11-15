using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
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
		/// <summary>
		/// Raised when the connection state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnConnectedStateChanged;

		/// <summary>
		/// Raised when the online state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnIsOnlineStateChanged;

		private readonly ISerialBuffer m_Buffer;
		private readonly object m_Parent;

		private readonly ConnectionStateManager m_ConnectionStateManager;

		private bool m_IsConnected;

		private bool m_IsOnline;

		public bool IsConnected
		{
			get { return m_IsConnected; }
			private set
			{
				if (m_IsConnected == value)
					return;

				m_IsConnected = value;

				OnConnectedStateChanged.Raise(this, new BoolEventArgs(value));
			}
		}

		public bool IsOnline
		{
			get { return m_IsOnline; }
			private set
			{
				if (m_IsOnline == value)
					return;

				m_IsOnline = value;

				OnIsOnlineStateChanged.Raise(this, new BoolEventArgs(m_IsOnline));
			}
		}

		/// <summary>
		/// Gets the id of the current serial port.
		/// </summary>
		public int? PortNumber { get { return m_ConnectionStateManager.PortNumber; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		public ClientSerialRpcController(object parent)
		{
			m_Buffer = new JsonSerialBuffer();
			m_Parent = parent;

			m_ConnectionStateManager = new ConnectionStateManager(this){ConfigurePort = ConfigurePort};
			Subscribe(m_ConnectionStateManager);

			Subscribe(m_Buffer);
		}

		#region Methods

		public void Connect()
		{
			m_ConnectionStateManager.Connect();
		}

		public void Disconnect()
		{
			m_ConnectionStateManager.Disconnect();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			OnConnectedStateChanged = null;
			OnIsOnlineStateChanged = null;

			Unsubscribe(m_Buffer);
			
			Unsubscribe(m_ConnectionStateManager);
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

		#region ConnectionStateManager Callbacks

		private void Subscribe(ConnectionStateManager connectionStateManger)
		{
			if (connectionStateManger == null)
				return;

			connectionStateManger.OnConnectedStateChanged += ConnectionStateMangerOnConnectedStateChanged;
			connectionStateManger.OnIsOnlineStateChanged += ConnectionStateMangerOnIsOnlineStateChanged;
			connectionStateManger.OnSerialDataReceived += ConnectionStateMangerOnSerialDataReceived;
		}

		private void Unsubscribe(ConnectionStateManager connectionStateManger)
		{
			if (connectionStateManger == null)
				return;

			connectionStateManger.OnConnectedStateChanged -= ConnectionStateMangerOnConnectedStateChanged;
			connectionStateManger.OnIsOnlineStateChanged -= ConnectionStateMangerOnIsOnlineStateChanged;
			connectionStateManger.OnSerialDataReceived -= ConnectionStateMangerOnSerialDataReceived;
		}

		private void ConnectionStateMangerOnSerialDataReceived(object sender, StringEventArgs args)
		{
			m_Buffer.Enqueue(args.Data);
		}

		private void ConnectionStateMangerOnIsOnlineStateChanged(object sender, BoolEventArgs args)
		{
			IsOnline = args.Data;
		}

		private void ConnectionStateMangerOnConnectedStateChanged(object sender, BoolEventArgs args)
		{
			IsConnected = args.Data;
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
