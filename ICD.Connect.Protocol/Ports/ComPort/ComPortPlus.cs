using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.Settings;
using ICD.Connect.Protocol.Utils;

namespace ICD.Connect.Protocol.Ports.ComPort
{
	/// <summary>
	/// ComPortPlus provides a bridge between S+ ComPorts and the S#/Pro libraries.
	/// </summary>
	public sealed class ComPortPlus : AbstractComPort<ComPortPlusSettings>
	{
		public event EventHandler<StringEventArgs> OnComSpecToChange;
		public event EventHandler<StringEventArgs> OnDataToSend;

		#region Properties

		/// <summary>
		/// The port index in S# Pro world (1 = first port).
		/// </summary>
		[PublicAPI]
		public ushort PortIndex { get; set; }

		/// <summary>
		/// Gets the Com Spec configuration properties.
		/// </summary>
		protected override IComSpecProperties ComSpecProperties
		{
			get
			{
				// TODO
				return new ComSpecProperties();
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Default constructor, sets index to 1.
		/// </summary>
		public ComPortPlus()
			: this(1)
		{
		}

		/// <summary>
		/// Constructor.
		/// Index 1 = first port.
		/// </summary>
		/// <param name="index"></param>
		public ComPortPlus(ushort index)
		{
			PortIndex = index;
			UpdateCachedOnlineStatus();
		}

		/// <summary>
		/// Convenience method for S+.
		/// Index 1 = first port.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		[PublicAPI]
		public static ComPortPlus Instantiate(ushort index)
		{
			return new ComPortPlus(index);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return true;
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnComSpecToChange = null;
			OnDataToSend = null;

			base.DisposeFinal(disposing);
		}

		/// <summary>
		/// Implements the actual sending logic. Wrapped by Send to handle connection status.
		/// </summary>
		protected override bool SendFinal(string data)
		{
			if (OnDataToSend == null)
				return false;

			OnDataToSend.Raise(this, new StringEventArgs(data));

			return true;
		}

		/// <summary>
		/// Raises the OnComSpecToChange event.
		/// 
		/// Raises 0 (success) if OnComSpecToChange has a listener (assumes the ComPort
		///		has been configured in S+)
		/// 
		/// Raises 1 (failure) if OnComSpecToChange has no listeners.
		/// </summary>
		/// <param name="baudRate"></param>
		/// <param name="numberOfDataBits"></param>
		/// <param name="parityType"></param>
		/// <param name="numberOfStopBits"></param>
		/// <param name="protocolType"></param>
		/// <param name="hardwareHandShake"></param>
		/// <param name="softwareHandshake"></param>
		/// <param name="reportCtsChanges"></param>
		/// <returns></returns>
		[PublicAPI]
		public void SetComPortSpec(ushort baudRate, ushort numberOfDataBits, ushort parityType, ushort numberOfStopBits,
		                           ushort protocolType, ushort hardwareHandShake, ushort softwareHandshake,
		                           ushort reportCtsChanges)
		{
			bool report = reportCtsChanges != 0;

			SetComPortSpec((eComBaudRates)baudRate, (eComDataBits)numberOfDataBits, (eComParityType)parityType,
			               (eComStopBits)numberOfStopBits, (eComProtocolType)protocolType,
			               (eComHardwareHandshakeType)hardwareHandShake,
			               (eComSoftwareHandshakeType)softwareHandshake, report);
		}

		/// <summary>
		/// Raises the OnComSpecToChange event.
		/// 
		/// Raises 0 (success) if OnComSpecToChange has a listener (assumes the ComPort
		///		has been configured in S+)
		/// 
		/// Raises 1 (failure) if OnComSpecToChange has no listeners.
		/// </summary>
		/// <param name="baudRate"></param>
		/// <param name="numberOfDataBits"></param>
		/// <param name="parityType"></param>
		/// <param name="numberOfStopBits"></param>
		/// <param name="protocolType"></param>
		/// <param name="hardwareHandShake"></param>
		/// <param name="softwareHandshake"></param>
		/// <param name="reportCtsChanges"></param>
		/// <returns></returns>
		[PublicAPI]
		public override void SetComPortSpec(eComBaudRates baudRate, eComDataBits numberOfDataBits, eComParityType parityType,
		                                    eComStopBits numberOfStopBits, eComProtocolType protocolType,
		                                    eComHardwareHandshakeType hardwareHandShake,
		                                    eComSoftwareHandshakeType softwareHandshake, bool reportCtsChanges)
		{
			string comSpec = ComSpecUtils.AssembleComSpec(PortIndex, baudRate, numberOfDataBits, parityType, numberOfStopBits,
			                                              protocolType, hardwareHandShake, softwareHandshake, reportCtsChanges);

			OnComSpecToChange.Raise(this, new StringEventArgs(comSpec));
		}

		#endregion
	}
}
