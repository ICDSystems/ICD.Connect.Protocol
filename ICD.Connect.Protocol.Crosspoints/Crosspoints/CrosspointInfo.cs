using System;
using System.ComponentModel;
using ICD.Common.Utils;
using ICD.Connect.Protocol.Ports;
using Newtonsoft.Json;

namespace ICD.Connect.Protocol.Crosspoints.Crosspoints
{
	/// <summary>
	/// Simple pairing of crosspoint id to host info.
	/// </summary>
	public struct CrosspointInfo : IEquatable<CrosspointInfo>
	{
		private readonly HostInfo m_Host;
		private readonly int m_Id;
		private readonly string m_Name;

		#region Properties

		/// <summary>
		/// Gets the address:port where the crosspoint lives.
		/// </summary>
		public HostInfo Host { get { return m_Host; } }

		/// <summary>
		/// Gets the id of the crosspoint.
		/// </summary>
		[DefaultValue(0)]
		public int Id { get { return m_Id; } }

		/// <summary>
		/// Gets the name of the crosspoint.
		/// </summary>
		[DefaultValue(null)]
		public string Name { get { return m_Name; } }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="name"></param>
		/// <param name="address"></param>
		/// <param name="port"></param>
		public CrosspointInfo(int id, string name, string address, ushort port)
			: this(id, name, new HostInfo(address, port))
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="name"></param>
		/// <param name="host"></param>
		[JsonConstructor]
		public CrosspointInfo(int id, string name, HostInfo host)
		{
			m_Id = id;
			m_Name = name;
			m_Host = host;
		}

		#endregion

		#region Methods

		public override string ToString()
		{
			ReprBuilder builder = new ReprBuilder(this);

			builder.AppendProperty("Id", m_Id);
			builder.AppendProperty("Name", m_Name);
			builder.AppendProperty("Host", m_Host);

			return builder.ToString();
		}

		/// <summary>
		/// Implementing default equality.
		/// </summary>
		/// <param name="s1"></param>
		/// <param name="s2"></param>
		/// <returns></returns>
		public static bool operator ==(CrosspointInfo s1, CrosspointInfo s2)
		{
			return s1.Equals(s2);
		}

		/// <summary>
		/// Implementing default inequality.
		/// </summary>
		/// <param name="s1"></param>
		/// <param name="s2"></param>
		/// <returns></returns>
		public static bool operator !=(CrosspointInfo s1, CrosspointInfo s2)
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
			return other is CrosspointInfo && Equals((CrosspointInfo)other);
		}

		public bool Equals(CrosspointInfo other)
		{
			return m_Id == other.m_Id &&
			       m_Name == other.m_Name &&
				   m_Host == other.m_Host;
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
				hash = hash * 23 + m_Id;
				hash = hash * 23 + (m_Name == null ? 0 : m_Name.GetHashCode());
// ReSharper disable once ImpureMethodCallOnReadonlyValueField
				hash = hash * 23 + m_Host.GetHashCode();
				return hash;
			}
		}

		#endregion
	}
}
