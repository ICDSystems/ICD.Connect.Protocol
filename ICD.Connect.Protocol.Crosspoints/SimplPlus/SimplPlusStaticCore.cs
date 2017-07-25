using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Connect.API.Nodes;
#if SIMPLSHARP
using ICD.Connect.API;
using ICD.Common.Logging.Console;
using ICD.Common.Logging.Console.Loggers;
using ICD.Common.Services;
using ICD.Common.Services.Logging;


namespace ICD.Connect.Protocol.Crosspoints.SimplPlus
{
	public static class SimplPlusStaticCore
	{
		/// <summary>
		/// Static Xp3 System that S+ modules can all access
		/// </summary>
		public static Xp3 Xp3Core { get; private set; }

		public static SimplPlusCrosspointWrapperManager WrapperManager { get; private set; }

		/// <summary>
		/// Constructor to create the Xp3
		/// </summary>
		static SimplPlusStaticCore()
		{
			// Set up logging.
			LoggingCore logger = new LoggingCore();
			logger.AddLogger(new IcdErrorLogger());
			logger.SeverityLevel = eSeverity.Warning;

			ServiceProvider.AddService<ILoggerService>(logger);

			// Create XP3 instance.
			Xp3Core = new Xp3();
			ApiConsole.RegisterChild(Xp3Core);

			// Create Wrapper instance
			WrapperManager = new SimplPlusCrosspointWrapperManager();
			ApiConsole.RegisterChild(WrapperManager);
		}
	}
}
#endif
