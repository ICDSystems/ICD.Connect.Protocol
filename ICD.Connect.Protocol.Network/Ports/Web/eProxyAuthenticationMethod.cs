using System;
using System.Linq;
#if SIMPLSHARP
using Crestron.SimplSharp.Net;
#endif
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;

namespace ICD.Connect.Protocol.Network.Ports.Web
{
	[Flags]
	public enum eProxyAuthenticationMethod
	{
		None = 0,
		Basic = 1,
		Digest = 2,
		Negotiate = 4,
		Ntlm = 8,
		DigestIe = 16,
		NtlmWb = 32,
		Only = 64,
		AnySafe = 128,
		Any = AnySafe | Basic
	}

	public static class ProxyAuthenticationMethodExtensions
	{
#if SIMPLSHARP
		private static readonly BiDictionary<eProxyAuthenticationMethod, AuthMethod> s_Map =
			new BiDictionary<eProxyAuthenticationMethod, AuthMethod>
			{
				{eProxyAuthenticationMethod.None, AuthMethod.NONE},
				{eProxyAuthenticationMethod.Basic, AuthMethod.BASIC},
				{eProxyAuthenticationMethod.Digest, AuthMethod.DIGEST},
				{eProxyAuthenticationMethod.Negotiate, AuthMethod.NEGOTIATE},
				{eProxyAuthenticationMethod.Ntlm, AuthMethod.NTLM},
				{eProxyAuthenticationMethod.DigestIe, AuthMethod.DIGEST_IE},
				{eProxyAuthenticationMethod.NtlmWb, AuthMethod.NTLM_WB},
				{eProxyAuthenticationMethod.Only, AuthMethod.ONLY},
				{eProxyAuthenticationMethod.AnySafe, AuthMethod.ANYSAFE},
				{eProxyAuthenticationMethod.Any, AuthMethod.ANY}
			};

		/// <summary>
		/// Converts the Crestron AuthMethod to an ICD Proxy Authentication Method.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static eProxyAuthenticationMethod ToIcd(this AuthMethod extends)
		{
			return EnumUtils.GetFlagsExceptNone(extends)
			                .Select(f => s_Map.GetKey(f))
			                .Aggregate(eProxyAuthenticationMethod.None, (current, flag) => current | flag);
		}

		/// <summary>
		/// Converts the ICD Proxy Authentication Method to a Crestron AuthMethod.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static AuthMethod ToCrestron(this eProxyAuthenticationMethod extends)
		{
			return EnumUtils.GetFlagsExceptNone(extends)
			                .Select(f => s_Map.GetValue(f))
			                .Aggregate(AuthMethod.NONE, (current, flag) => current | flag);
		}
#endif
	}
}
