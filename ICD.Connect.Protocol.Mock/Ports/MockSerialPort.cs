using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.Mock.Ports
{
	public sealed class MockSerialPort : AbstractSerialPort<MockSerialPortSettings>
	{
		[PublicAPI]
		public event EventHandler<StringEventArgs> OnSend; 

		/// <summary>
		/// Connects to the end point.
		/// </summary>
		public override void Connect()
		{
			IsConnected = true;
		}

		/// <summary>
		/// Disconnects from the end point.
		/// </summary>
		public override void Disconnect()
		{
			IsConnected = false;
		}

		/// <summary>
		/// Returns the connection state of the port
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsConnectedState()
		{
			return IsConnected;
		}

		/// <summary>
		/// Implements the actual sending logic. Wrapped by Send to handle connection status.
		/// </summary>
		protected override bool SendFinal(string data)
		{
			EventHandler<StringEventArgs> handler = OnSend;
			if (handler == null)
				return false;

			handler(this, new StringEventArgs(data));

			return true;
		}
	}
}