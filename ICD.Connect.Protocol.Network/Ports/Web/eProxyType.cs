#if SIMPLSHARP
using Crestron.SimplSharp.Net;
#endif
using ICD.Common.Utils.Collections;

namespace ICD.Connect.Protocol.Network.Ports.Web
{
	public enum eProxyType
	{
		None,
		Http,
		Http1,
		Socks4,
		Soscks4A,
		Socks5,
		Socks5H,
	}

	public static class ProxyTypeExtensions
	{
#if SIMPLSHARP
		private static readonly BiDictionary<eProxyType, ProxyType> s_Map =
			new BiDictionary<eProxyType, ProxyType>
			{
				{eProxyType.None, ProxyType.NONE},
				{eProxyType.Http, ProxyType.HTTP},
				{eProxyType.Http1, ProxyType.HTTP_1_0},
				{eProxyType.Socks4, ProxyType.SOCKS4},
				{eProxyType.Soscks4A, ProxyType.SOCKS4A},
				{eProxyType.Socks5, ProxyType.SOCKS5},
				{eProxyType.Socks5H, ProxyType.SOCKS5H}
			};

		/// <summary>
		/// Converts the Crestron ProxyType to an ICD Proxy Type.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static eProxyType ToIcd(this ProxyType extends)
		{
			return s_Map.GetKey(extends);
		}

		/// <summary>
		/// Converts the ICD Proxy Type to a Crestron ProxyType.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static ProxyType ToCrestron(this eProxyType extends)
		{
			return s_Map.GetValue(extends);
		}
#endif
	}
}
