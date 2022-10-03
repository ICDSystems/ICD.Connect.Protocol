#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using System;
using System.Text.RegularExpressions;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Json;

namespace ICD.Connect.Protocol.Ports
{
	[JsonConverter(typeof(ToStringJsonConverter))]
	public struct HostSessionInfo : IEquatable<HostSessionInfo>
	{
		private const string HOSTSESSIONINFO_REGEX =
			@"^(?'host'\S+):(?'port'\d+):(?'session'[{(]?[0-9A-F]{8}[-]?(?:[0-9A-F]{4}[-]?){3}[0-9A-F]{12}[)}]?)$";

		private readonly HostInfo m_Host;
		private readonly Guid m_Session;

		#region Properties

		/// <summary>
		/// Gets the host info for the remote endpoint.
		/// </summary>
		public HostInfo Host { get { return m_Host; } }

		/// <summary>
		/// Gets the unique session id for the remote endpoint.
		/// </summary>
		public Guid Session { get { return m_Session; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="host"></param>
		/// <param name="session"></param>
		public HostSessionInfo(HostInfo host, Guid session)
		{
			m_Host = host;
			m_Session = session;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="hostname"></param>
		/// <param name="port"></param>
		/// <param name="session"></param>
		public HostSessionInfo(string hostname, ushort port, Guid session)
			: this(new HostInfo(hostname, port), session)
		{
		}

		#region Methods

		/// <summary>
		/// Returns the HostInfo in the format "Address:Port".
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return string.Format("{0}:{1}", m_Host, m_Session);
		}

		/// <summary>
		/// Implementing default equality.
		/// </summary>
		/// <param name="s1"></param>
		/// <param name="s2"></param>
		/// <returns></returns>
		public static bool operator ==(HostSessionInfo s1, HostSessionInfo s2)
		{
			return s1.Equals(s2);
		}

		/// <summary>
		/// Implementing default inequality.
		/// </summary>
		/// <param name="s1"></param>
		/// <param name="s2"></param>
		/// <returns></returns>
		public static bool operator !=(HostSessionInfo s1, HostSessionInfo s2)
		{
			return !s1.Equals(s2);
		}

		public override bool Equals(object obj)
		{
			return obj is HostSessionInfo && Equals((HostSessionInfo)obj);
		}

		public bool Equals(HostSessionInfo other)
		{
			return m_Host == other.m_Host &&
			       m_Session == other.m_Session;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (m_Host.GetHashCode() * 397) ^ m_Session.GetHashCode();
			}
		}

		[UsedImplicitly]
		public static HostSessionInfo Parse(string data)
		{
			HostSessionInfo output;
			if (!TryParse(data, out output))
				throw new FormatException("Expected data in HOST:PORT:GUID format, got " + StringUtils.ToRepresentation(data));

			return output;
		}

		public static bool TryParse(string data, out HostSessionInfo info)
		{
			info = default(HostSessionInfo);

			if (data == null)
				return false;

			Match match;
			if (!RegexUtils.Matches(data, HOSTSESSIONINFO_REGEX, RegexOptions.IgnoreCase, out match))
				return false;

			string hostname = match.Groups["host"].Value;
			ushort port = ushort.Parse(match.Groups["port"].Value);
			Guid session = new Guid(match.Groups["session"].Value);

			info = new HostSessionInfo(hostname, port, session);

			return true;
		}

		#endregion
	}
}
