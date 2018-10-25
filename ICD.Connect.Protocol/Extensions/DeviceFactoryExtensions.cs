using ICD.Common.Properties;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Protocol.Extensions
{
	public static class DeviceFactoryExtensions
	{
		/// <summary>
		/// Lazy-loads the port with the given id.
		/// </summary>
		/// <param name="factory"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		[PublicAPI]
		[NotNull]
		public static IPort GetPortById(this IDeviceFactory factory, int id)
		{
			return factory.GetOriginatorById<IPort>(id);
		}
	}
}
