﻿using System.Collections.Generic;
using ICD.Connect.Protocol.Ports.IrPort;
using ICD.Connect.Protocol.Ports.IrPort.IrPulse;
using ICD.Connect.Protocol.Settings;

namespace ICD.Connect.Protocol.Mock.Ports.IrPort
{
	public sealed class MockIrPort : AbstractIrPort<MockIrPortSettings>
	{
		private readonly IrDriverProperties m_IrDriverProperties;

		private string m_DriverPath;

		#region Properties

		/// <summary>
		/// Controls pulsing and timing for the IR port.
		/// </summary>
		public override IrPortPulseComponent PulseComponent { get; set; }

		/// <summary>
		/// Gets the IR Driver configuration properties.
		/// </summary>
		public override IIrDriverProperties IrDriverProperties { get { return m_IrDriverProperties; } }

		/// <summary>
		/// Gets the path to the loaded IR driver.
		/// </summary>
		public override string DriverPath { get { return m_DriverPath; } }

		/// <summary>
		/// Gets/sets the default pulse time in milliseconds for a PressAndRelease.
		/// </summary>
		public override ushort PulseTime { get; set; }

		/// <summary>
		/// Gets/sets the default time in milliseconds between PressAndRelease commands.
		/// </summary>
		public override ushort BetweenTime { get; set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public MockIrPort()
		{
			m_IrDriverProperties = new IrDriverProperties();
		}

		#region Methods

		/// <summary>
		/// Loads the driver from the given path.
		/// </summary>
		/// <param name="path"></param>
		public override void LoadDriver(string path)
		{
			m_DriverPath = path;
		}

		/// <summary>
		/// Gets the loaded IR commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<string> GetCommands()
		{
			yield break;
		}

		/// <summary>
		/// Begin sending the command.
		/// </summary>
		/// <param name="command"></param>
		public override void Press(string command)
		{
		}

		/// <summary>
		/// Stop sending the current command.
		/// </summary>
		public override void Release()
		{
		}

		#endregion

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return true;
		}
	}
}
