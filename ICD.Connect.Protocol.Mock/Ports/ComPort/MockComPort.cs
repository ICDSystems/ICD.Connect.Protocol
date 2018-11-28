using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.Settings;

namespace ICD.Connect.Protocol.Mock.Ports.ComPort
{
	public sealed class MockComPort : AbstractComPort<MockComPortSettings>
	{
		[PublicAPI]
		public event EventHandler<StringEventArgs> OnSend;

		private readonly ComSpecProperties m_ComSpecProperties;

		/// <summary>
		/// Gets the Com Spec configuration properties.
		/// </summary>
		protected override IComSpecProperties ComSpecProperties { get { return m_ComSpecProperties; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		public MockComPort()
		{
			m_ComSpecProperties = new ComSpecProperties();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnSend = null;

			base.DisposeFinal(disposing);
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

		/// <summary>
		/// Configures the ComPort for communication.
		/// </summary>
		/// <param name="comSpec"></param>
		public override void SetComPortSpec(ComSpec comSpec)
		{
		}
	}
}
