﻿using ICD.Common.Logging;
using ICD.Common.Logging.Loggers;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Crosspoints.Tests
{
	[SetUpFixture]
    public sealed class Setup
	{
		private LoggingCore m_Logger;

		[OneTimeSetUp]
		public void Init()
		{
			m_Logger = new LoggingCore();
			m_Logger.AddLogger(new IcdErrorLogger());
			m_Logger.SeverityLevel = eSeverity.Debug;

			ServiceProvider.AddService<ILoggerService>(m_Logger);
		}

		[OneTimeTearDown]
		public void Deinit()
		{
			ServiceProvider.RemoveService<ILoggerService>(m_Logger);
		}
	}
}
