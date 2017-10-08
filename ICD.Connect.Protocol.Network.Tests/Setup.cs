using ICD.Common.Logging.Console;
using ICD.Common.Logging.Console.Loggers;
using ICD.Common.Services;
using ICD.Common.Services.Logging;
using NUnit.Framework;

namespace ICD.Connect.Protocol.Network.Tests
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
