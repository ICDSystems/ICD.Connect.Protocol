using System.Collections;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Protocol.Sigs
{
	/// <summary>
	/// Provides unique collection of sigs, ignoring serial/analog/digital values for comparison.
	/// </summary>
	[PublicAPI]
	public sealed class SigCache : ICollection<SigInfo>
	{
		/// <summary>
		/// We cache the sigs by their type and address.
		/// </summary>
		private struct SigKey
		{
			private readonly eSigType m_Type;
			private readonly uint m_Number;
			private readonly string m_Name;
			private readonly ushort m_SmartObject;

			private SigKey(eSigType type, uint number, string name, ushort smartObject)
			{
				m_Type = type;
				m_Number = number;
				m_Name = name;
				m_SmartObject = smartObject;
			}

			public static SigKey FromSig(SigInfo sigInfo)
			{
				return new SigKey(sigInfo.Type, sigInfo.Number, sigInfo.Name, sigInfo.SmartObject);
			}

			#region Equality

			public override bool Equals(object obj)
			{
				return obj is SigKey && this == (SigKey)obj;
			}

			public override int GetHashCode()
			{
				unchecked
				{
					int hash = 17;

					hash = hash * 23 + (int)m_Type;
					hash = hash * 23 + (int)m_Number;
					hash = hash * 23 + (m_Name == null ? 0 : m_Name.GetHashCode());
					hash = hash * 23 + m_SmartObject;

					return hash;
				}
			}

			public static bool operator ==(SigKey x, SigKey y)
			{
				bool output = x.m_Type == y.m_Type &&
							  x.m_Number == y.m_Number &&
							  x.m_Name == y.m_Name &&
							  x.m_SmartObject == y.m_SmartObject;

				return output;
			}

			public static bool operator !=(SigKey x, SigKey y)
			{
				return !(x == y);
			}

			#endregion
		}

		// The internal collection
		private readonly Dictionary<SigKey, SigInfo> m_KeyToSig; 

		#region Properties

		public int Count { get { return m_KeyToSig.Count; } }
		public bool IsReadOnly { get { return false; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public SigCache()
		{
			m_KeyToSig = new Dictionary<SigKey, SigInfo>();
		}

		#region Methods

		public IEnumerator<SigInfo> GetEnumerator()
		{
			return m_KeyToSig.Values.GetEnumerator();
		}

		public void Add(SigInfo item)
		{
			m_KeyToSig[SigKey.FromSig(item)] = item;
		}

		public void AddRange(IEnumerable<SigInfo> sigs)
		{
			sigs.ForEach(Add);
		}

		/// <summary>
		/// Adds sigs that have a value, removes sigs that do not have a value
		/// </summary>
		/// <param name="sigs"></param>
		public void AddHighRemoveLow(IEnumerable<SigInfo> sigs)
		{
			sigs.ForEach(AddHighRemoveLow);
		}

		/// <summary>
		/// Adds the sig if it has a value, otherwise removes it.
		/// </summary>
		/// <param name="sigInfo"></param>
		public void AddHighRemoveLow(SigInfo sigInfo)
		{
			if (sigInfo.HasValue())
				Add(sigInfo);
			else
				Remove(sigInfo);
		}

		/// <summary>
		/// Adds clear version of sigs that have a value, removes sigs that do not have a value
		/// </summary>
		/// <param name="sigs"></param>
		public void AddHighClearRemoveLow(IEnumerable<SigInfo> sigs)
		{
			sigs.ForEach(AddHighClearRemoveLow);
		}

		/// <summary>
		/// Adds clear version of the sig if it has a value, otherwise removes it.
		/// </summary>
		/// <param name="sigInfo"></param>
		public void AddHighClearRemoveLow(SigInfo sigInfo)
		{
			if (sigInfo.HasValue())
				Add(sigInfo.ToClearSig());
			else
				Remove(sigInfo);
		}

		public void Clear()
		{
			m_KeyToSig.Clear();
		}

		public bool Contains(SigInfo item)
		{
			return m_KeyToSig.ContainsKey(SigKey.FromSig(item));
		}

		public void CopyTo(SigInfo[] array, int arrayIndex)
		{
			m_KeyToSig.Values.CopyTo(array, arrayIndex);
		}

		public bool Remove(SigInfo item)
		{
			return m_KeyToSig.Remove(SigKey.FromSig(item));
		}

		public void RemoveRange(IEnumerable<SigInfo> sigs)
		{
			foreach (var s in sigs)
			{
				Remove(s);
			}
		}

		#endregion

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
