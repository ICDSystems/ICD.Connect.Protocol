using ICD.Common.Logging;
using ICD.Common.Logging.Loggers;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API;

namespace ICD.Connect.Protocol.Crosspoints.SimplPlus
{
	public static class SimplPlusStaticCore
	{
		/// <summary>
		/// Static Xp3 System that S+ modules can all access
		/// </summary>
		public static Xp3 Xp3Core { get; private set; }

		public static SimplPlusCrosspointShimManager ShimManager { get; private set; }

		/// <summary>
		/// Constructor to create the Xp3
		/// </summary>
		static SimplPlusStaticCore()
		{
			// Set up logging.
			LoggingCore logger = new LoggingCore();
			logger.AddLogger(new IcdErrorLogger());
			logger.SeverityLevel = eSeverity.Warning;

			ServiceProvider.TryAddService<ILoggerService>(logger);

			// Create XP3 instance.
			Xp3Core = new Xp3();
			ApiConsole.RegisterChild(Xp3Core);

			// Create Shim instance
			ShimManager = new SimplPlusCrosspointShimManager();
			ApiConsole.RegisterChild(ShimManager);

			ShimManager.RegisterXp3(Xp3Core);
		}
	}
}
