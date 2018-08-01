using System;
using System.ComponentModel;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using Newtonsoft.Json;

namespace ICD.Connect.Protocol.Ports
{
	/// <summary>
	/// Simple pairing of hostname and port.
	/// </summary>
	public struct HostInfo : IEquatable<HostInfo>
	{
		private readonly string m_Address;
		private readonly ushort m_Port;

		#region Properties

		/// <summary>
		/// Gets the hostname.
		/// </summary>
		[DefaultValue(null)]
		public string Address { get { return m_Address; } }

		/// <summary>
		/// Gets the port.
		/// </summary>
		[DefaultValue(0)]
		public ushort Port { get { return m_Port; } }

		/// <summary>
		/// Gets the hostname. Returns localhost if the address points back to this processor.
		/// </summary>
		[PublicAPI, JsonIgnore]
		public string AddressOrLocalhost
		{
			get { return IsLocalHost ? "127.0.0.1" : m_Address; }
		}

		/// <summary>
		/// Returns true if the address represents the localhost.
		/// </summary>
		[PublicAPI, JsonIgnore]
		public bool IsLocalHost
		{
			get { return IcdEnvironment.NetworkAddresses.Contains(m_Address); }
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="address"></param>
		/// <param name="port"></param>
		[JsonConstructor]
		public HostInfo(string address, ushort port)
		{
			m_Address = address;
			m_Port = port;
		}

		#region Methods

		/// <summary>
		/// Returns the HostInfo in the format "Address:Port".
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return string.Format("{0}:{1}", m_Address, m_Port);
		}

		/// <summary>
		/// Implementing default equality.
		/// </summary>
		/// <param name="s1"></param>
		/// <param name="s2"></param>
		/// <returns></returns>
		public static bool operator ==(HostInfo s1, HostInfo s2)
		{
			return s1.Equals(s2);
		}

		/// <summary>
		/// Implementing default inequality.
		/// </summary>
		/// <param name="s1"></param>
		/// <param name="s2"></param>
		/// <returns></returns>
		public static bool operator !=(HostInfo s1, HostInfo s2)
		{
			return !s1.Equals(s2);
		}

		/// <summary>
		/// Returns true if this instance is equal to the given object.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool Equals(object other)
		{
			return other is HostInfo && Equals((HostInfo)other);
		}

		public bool Equals(HostInfo other)
		{
			return m_Port == other.m_Port &&
			       m_Address == other.m_Address;
		}

		/// <summary>
		/// Gets the hashcode for this instance.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 23 + m_Port;
				hash = hash * 23 + (m_Address == null ? 0 : AddressOrLocalhost.GetHashCode());
				return hash;
			}
		}

		public static bool TryParse(string str, out HostInfo info)
		{
			try
			{
				int index = str.IndexOf(':');
				ushort port = ushort.Parse(str.Substring(index + 1, str.Length - index - 1));
				info = new HostInfo(str.Substring(0, index), port);
				return true;
			}
			catch (Exception)
			{
				info = default(HostInfo);
				return false;
			}
		}

		#endregion
	}
}
