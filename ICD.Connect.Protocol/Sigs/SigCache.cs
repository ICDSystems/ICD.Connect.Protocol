using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Protocol.Sigs
{
	/// <summary>
	/// Provides unique collection of sigs, ignoring serial/analog/digital values for comparison.
	/// </summary>
	[JsonConverter(typeof(SigCacheConverter))]
	public sealed class SigCache : IEnumerable<SigInfo>
	{
		private readonly Dictionary<eSigType, Dictionary<ushort, Dictionary<uint, object>>> m_KeyToSig;

		#region Properties

		public int Count { get { return m_KeyToSig.Count; } }

		public bool IsReadOnly { get { return false; } }

		internal Dictionary<eSigType, Dictionary<ushort, Dictionary<uint, object>>> KeyToSig { get { return m_KeyToSig; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public SigCache()
		{
			m_KeyToSig = new Dictionary<eSigType, Dictionary<ushort, Dictionary<uint, object>>>();
		}

		#region Methods

		public IEnumerator<SigInfo> GetEnumerator()
		{
			return m_KeyToSig.SelectMany(
				typeKvp => typeKvp.Value.SelectMany(
					soKvp => soKvp.Value.Select(
						numKvp => SigInfo.FromObject(numKvp.Key, soKvp.Key, numKvp.Value)
						)
					)
				).GetEnumerator();
		}

		public bool Add(SigInfo item)
		{
			Dictionary<ushort, Dictionary<uint, object>> sigTypes;
			if (!m_KeyToSig.TryGetValue(item.Type, out sigTypes))
			{
				sigTypes = new Dictionary<ushort, Dictionary<uint, object>>();
				m_KeyToSig.Add(item.Type, sigTypes);
			}

			Dictionary<uint, object> smartObjects;
			if (!sigTypes.TryGetValue(item.SmartObject, out smartObjects))
			{
				smartObjects = new Dictionary<uint, object>();
				sigTypes.Add(item.SmartObject, smartObjects);
			}

			if (smartObjects.ContainsKey(item.Number))
				return false;

			smartObjects.Add(item.Number, item.GetValue());

			return true;
		}

		public void AddRange(IEnumerable<SigInfo> sigs)
		{
			foreach (SigInfo sig in sigs)
				Add(sig);
		}

		/// <summary>
		/// Adds sigs that have a value, removes sigs that do not have a value
		/// </summary>
		/// <param name="sigs"></param>
		public void AddHighRemoveLow(IEnumerable<SigInfo> sigs)
		{
			foreach (SigInfo sig in sigs)
				AddHighRemoveLow(sig);
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

		public bool Remove(SigInfo item)
		{
			Dictionary<ushort, Dictionary<uint, object>> sigTypes;
			if (!m_KeyToSig.TryGetValue(item.Type, out sigTypes))
				return false;

			Dictionary<uint, object> smartObjects;
			if (!sigTypes.TryGetValue(item.SmartObject, out smartObjects))
				return false;

			if (!smartObjects.Remove(item.Number))
				return false;

			if (smartObjects.Count == 0)
				sigTypes.Remove(item.SmartObject);

			if (sigTypes.Count == 0)
				m_KeyToSig.Remove(item.Type);

			return true;
		}

		public void RemoveRange(IEnumerable<SigInfo> sigs)
		{
			foreach (SigInfo s in sigs)
				Remove(s);
		}

		#endregion

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
