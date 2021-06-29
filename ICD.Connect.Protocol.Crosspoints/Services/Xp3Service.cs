using ICD.Common.Utils.Services;

namespace ICD.Connect.Protocol.Crosspoints.Services
{
	public static class Xp3Service
	{
		private static Xp3 s_CachedXp3;

		public static Xp3 Xp3
		{
			get
			{
				if (s_CachedXp3 != null)
					return s_CachedXp3;

				if (ServiceProvider.TryGetService<Xp3>() == null)
					ServiceProvider.AddService(new Xp3());

				return s_CachedXp3 = ServiceProvider.GetService<Xp3>();
			}
		}
	}
}
