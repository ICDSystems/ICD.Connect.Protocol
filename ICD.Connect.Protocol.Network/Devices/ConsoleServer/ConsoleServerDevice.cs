using System;
using System.Text.RegularExpressions;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.API;
using ICD.Connect.Devices;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Network.Ports.Tcp;
using ICD.Connect.Protocol.Network.Servers;
using ICD.Connect.Settings;

namespace ICD.Connect.Protocol.Network.Devices.ConsoleServer
{
	public sealed class ConsoleServerDevice : AbstractDevice<ConsoleServerSettings>
	{
		private const string NEWLINE_REGEX = "(?<!\r)\n";

		[NotNull]
		private readonly IcdTcpServer m_TcpServer;
		
		/// <summary>
		/// Constructor.
		/// </summary>
		public ConsoleServerDevice()
		{
			m_TcpServer = new IcdTcpServer
			{
				Port = ConsoleServerSettings.DEFAULT_PORT
			};
			Subscribe(m_TcpServer);

			IcdConsole.OnConsolePrint += IcdConsoleOnConsolePrint;
		}

		public ushort Port
		{
			get { return m_TcpServer.Port; }
			set
			{
				if (value == m_TcpServer.Port)
					return;

				m_TcpServer.Port = value;

				m_TcpServer.Restart();
			}
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			m_TcpServer.Dispose();

			IcdConsole.OnConsolePrint -= IcdConsoleOnConsolePrint;
		}

		#region Private Methods

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			// ReSharper disable ConditionIsAlwaysTrueOrFalse
			return m_TcpServer != null && m_TcpServer.Listening;
			// ReSharper restore ConditionIsAlwaysTrueOrFalse
		}

		/// <summary>
		/// Sends the message to all clients.
		/// </summary>
		/// <param name="message"></param>
		private void Send(string message)
		{
			string sanitized = Regex.Replace(message, NEWLINE_REGEX, "\r\n");
			
			m_TcpServer.Send(sanitized);
		}

		/// <summary>
		/// Sends the message to the given client.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="message"></param>
		private void Send(uint client, string message)
		{
			if (!m_TcpServer.ClientConnected(client))
				return;

			string sanitized = Regex.Replace(message, NEWLINE_REGEX, "\r\n");

			m_TcpServer.Send(client, sanitized);
		}

		/// <summary>
		/// Sends the message as a line to the given client.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="message"></param>
		private void SendLine(uint client, string message)
		{
			if (!m_TcpServer.ClientConnected(client))
				return;

			string sanitized = Regex.Replace(message, NEWLINE_REGEX, "\r\n");
			string printLine = sanitized + "\r\n";

			m_TcpServer.Send(client, printLine);
		}

		#endregion

		#region TCP Server Callbacks

		private void Subscribe(IcdTcpServer tcpServer)
		{
			tcpServer.OnSocketStateChange += TcpServerOnSocketStateChange;
			tcpServer.OnDataReceived += TcpServerOnDataReceived;
			tcpServer.OnListeningStateChanged += TcpServerOnListeningStateChanged;
		}

		private void Unsubscribe(IcdTcpServer tcpServer)
		{
			tcpServer.OnSocketStateChange -= TcpServerOnSocketStateChange;
			tcpServer.OnDataReceived -= TcpServerOnDataReceived;
			tcpServer.OnListeningStateChanged -= TcpServerOnListeningStateChanged;
		}

		private void TcpServerOnSocketStateChange(object sender, SocketStateEventArgs args)
		{
			if (args.SocketState != SocketStateEventArgs.eSocketStatus.SocketStatusConnected || args.ClientId == 0)
				return;

			SendLine(args.ClientId, "Welcome to ICD Console. All commands begin with `ICD`. Type `ICD ?` for help.");
		}

		private void TcpServerOnDataReceived(object sender, DataReceiveEventArgs args)
		{
			string commandString = args.Data.Trim();

			// Treat empty command as a help command
			commandString = string.IsNullOrEmpty(commandString) ? ApiConsole.HELP_COMMAND : commandString;

			// User convenience, let them know there's actually a UCMD handler
			if (commandString == ApiConsole.HELP_COMMAND)
			{
				Send(args.ClientId, "> ");
				return;
			}

			// Only care about commands that start with ICD prefix.
			if (!commandString.Equals(ApiConsole.ROOT_COMMAND, StringComparison.OrdinalIgnoreCase) &&
				!commandString.StartsWith(ApiConsole.ROOT_COMMAND + ' ', StringComparison.OrdinalIgnoreCase))
				return;

			// Trim the prefix
			commandString = commandString.Substring(ApiConsole.ROOT_COMMAND.Length).Trim();

			// Execute the command
			string result = ApiConsole.ExecuteCommandForResponse(commandString);

			SendLine(args.ClientId, result);
		}

		private void TcpServerOnListeningStateChanged(object sender, BoolEventArgs e)
		{
			UpdateCachedOnlineStatus();
		}

		#endregion

		#region ICDConsole Callback

		private void IcdConsoleOnConsolePrint(object sender, StringEventArgs args)
		{
			Send(args.Data);
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(ConsoleServerSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Port = Port;
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(ConsoleServerSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			Port = settings.Port;
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			Port = ConsoleServerSettings.DEFAULT_PORT;
		}

		#endregion
	}
}
