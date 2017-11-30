﻿using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Protocol.Ports.ComPort;

namespace ICD.Connect.Protocol.Mock.Ports.ComPort
{
	public sealed class MockComPort : AbstractComPort<MockComPortSettings>
	{
		[PublicAPI]
		public event EventHandler<StringEventArgs> OnSend;

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

		public override void SetComPortSpec(eComBaudRates baudRate, eComDataBits numberOfDataBits, eComParityType parityType,
		                                    eComStopBits numberOfStopBits, eComProtocolType protocolType,
		                                    eComHardwareHandshakeType hardwareHandShake, eComSoftwareHandshakeType softwareHandshake,
		                                    bool reportCtsChanges)
		{
		}
	}
}