using System;
using System.Text.RegularExpressions;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Protocol.Network.Servers;
using ICD.Connect.Protocol.Ports.IrPort;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.Settings;
using ICD.Connect.Settings;

namespace ICD.Connect.Protocol.Network.Devices.PortServers
{
	/// <summary>
	/// Press(PLAY)\x0d
	/// Release()\x0d
	/// PressAndRelease(PLAY)\x0d
	/// PressAndRelease(PLAY, 50)\x0d
	/// PressAndRelease(PLAY, 50, 500)\x0d
	/// </summary>
	public sealed class IrPortServerDevice : AbstractPortServerDevice<IIrPort, IrPortServerDeviceSettings>
	{
		private const char COMMAND_DELIMITER = '\x0D';
		private const char PARAMETER_DELIMITER = ',';
		private static readonly char[] s_trimChars = { ' ', '\x0d', '\x0a' };

		private readonly Regex m_CommandRegex;

		private readonly DelimiterSerialBuffer m_IncomingBuffer;
		private readonly IrDriverProperties m_IrDriverProperties;

		public IrPortServerDevice()
		{
			m_CommandRegex = new Regex(@"([^\(]+)\(([^)]+)*\)");

			m_IncomingBuffer = new DelimiterSerialBuffer(COMMAND_DELIMITER);
			m_IrDriverProperties = new IrDriverProperties();

			Subscribe(m_IncomingBuffer);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			Unsubscribe(m_IncomingBuffer);

		}

		/// <summary>
		/// Called after the port is subscribed to
		/// Run any action needed to set or configure the port here
		/// </summary>
		/// <param name="port"></param>
		protected override void SetPortInternal(IIrPort port)
		{
			base.SetPortInternal(port);

			ConfigurePort(port);
		}

		/// <summary>
		/// Configures the given port for communication with the device.
		/// </summary>
		/// <param name="port"></param>
		private void ConfigurePort(IIrPort port)
		{
			if (port != null)
				port.ApplyDeviceConfiguration(m_IrDriverProperties);
		}

		#region Serial Buffer Callbacks

		private void Subscribe(ISerialBuffer buffer)
		{
			if (buffer == null)
				return;

			buffer.OnCompletedSerial += BufferOnCompletedSerial;
		}

		private void Unsubscribe(ISerialBuffer buffer)
		{
			if (buffer == null)
				return;

			buffer.OnCompletedSerial -= BufferOnCompletedSerial;
		}

		private void BufferOnCompletedSerial(object sender, StringEventArgs args)
		{
			Match match = m_CommandRegex.Match(args.Data.Trim(s_trimChars));
			if (!match.Success)
			{
				Logger.Log(eSeverity.Error, "Could not parse IrPortServer command from {0}", args.Data);
				return;
			}

			string command = match.Groups[1].Value.Trim(s_trimChars);
			string commandParams = match.Groups[2].Success ? match.Groups[2].Value.Trim(s_trimChars) : null;

			ProcessCommand(command, commandParams);
		}

		private void ProcessCommand(string command, string commandParams)
		{
			if (Port == null)
			{
				Logger.Log(eSeverity.Error, "Could not send command - port null");
				return;
			}

			string[] paramsArray = string.IsNullOrEmpty(commandParams)
				                       ? new string[0] : commandParams.Split(PARAMETER_DELIMITER);

			switch (command.ToLower())
			{
				case "press":
					ProcessPress(paramsArray);
					break;
				case "release":
					ProcessRelease();
					break;
				case "pressandrelease":
					ProcessPressAndRelease(paramsArray);
					break;
				default:
					Logger.Log(eSeverity.Warning, "Command {0} not matched", command);
					break;
			}
		}

		private void ProcessPress(string[] paramsArray)
		{
			if (paramsArray.Length != 1)
			{
				Logger.Log(eSeverity.Error, "Wrong number of parameters for Press - {0}", paramsArray.Join(","));
				return;
			}

			if (Port == null)
				return;

			Port.Press(paramsArray[0]);
		}

		private void ProcessRelease()
		{
			if (Port != null)
				Port.Release();
		}

		private void ProcessPressAndRelease(string[] paramsArray)
		{
			if (Port == null)
				return;

			if (paramsArray.Length == 1)
			{
				Port.PressAndRelease(paramsArray[0]);
				return;
			}

			ushort holdTime;
			ushort betweenTime = 0;

			if (paramsArray.Length == 3)
			{
				try
				{
					betweenTime = ushort.Parse(paramsArray[2]);
				}
				catch (FormatException)
				{
					Logger.Log(eSeverity.Error, "Couldn't parse betweenTime as ushort: {0}", paramsArray[1]);
					return;
				}
			}

			if (paramsArray.Length == 2 || paramsArray.Length == 3)
			{
				try
				{
					holdTime = ushort.Parse(paramsArray[1]);
				}
				catch (FormatException)
				{
					Logger.Log(eSeverity.Error, "Couldn't parse holdTime as ushort: {0}", paramsArray[1]);
					return;
				}

				if (betweenTime == 0)
					Port.PressAndRelease(paramsArray[0], holdTime);
				else
					Port.PressAndRelease(paramsArray[0], holdTime, betweenTime);
				return;
			}

			Logger.Log(eSeverity.Error, "Wrong number of parameters for PressAndRelease - {0}",
			           paramsArray.Join(","));
		}

		#endregion

		#region TCPServer Callbacks

		protected override void IncomingServerOnDataReceived(object sender, DataReceiveEventArgs args)
		{
			m_IncomingBuffer.Enqueue(args.Data);
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(IrPortServerDeviceSettings settings)
		{
			base.CopySettingsFinal(settings);
			
			settings.Copy(m_IrDriverProperties);
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			m_IrDriverProperties.ClearIrProperties();

			m_IncomingBuffer.Clear();
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(IrPortServerDeviceSettings settings, IDeviceFactory factory)
		{
			// grab IR driver properties first so port gets configured when set
			m_IrDriverProperties.Copy(settings);

			base.ApplySettingsFinal(settings, factory);
		}

		#endregion
	}
}
