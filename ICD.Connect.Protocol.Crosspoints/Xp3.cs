using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Protocol.Crosspoints
{
	/// <summary>
	/// Xp3 provides a top-level collection of independent CrosspointSystems.
	/// </summary>
	public sealed class Xp3 : IDisposable, IConsoleNode
	{
		private readonly Dictionary<int, CrosspointSystem> m_Systems;
		private readonly SafeCriticalSection m_SystemsSection;

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public string ConsoleName { get { return GetType().Name; } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public string ConsoleHelp { get { return "The top level Crosspoint manager object"; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		public Xp3()
		{
			m_Systems = new Dictionary<int, CrosspointSystem>();
			m_SystemsSection = new SafeCriticalSection();
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			Clear();
		}

		/// <summary>
		/// Destroys all of the existing crosspoint systems.
		/// </summary>
		public void Clear()
		{
			foreach (int id in GetSystemIds())
				RemoveSystem(id);
		}

		/// <summary>
		/// Gets the available system ids.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<int> GetSystemIds()
		{
			return m_SystemsSection.Execute(() => m_Systems.Keys.Order().ToArray());
		}

		/// <summary>
		/// Gets the available systems.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<CrosspointSystem> GetSystems()
		{
			return m_SystemsSection.Execute(() => m_Systems.OrderValuesByKey().ToArray());
		}

		/// <summary>
		/// Gets the system with the given id.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public CrosspointSystem GetSystem(int id)
		{
			return m_SystemsSection.Execute(() => m_Systems[id]);
		}

		/// <summary>
		/// Creates a new system with the given id.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public CrosspointSystem CreateSystem(int id)
		{
			m_SystemsSection.Enter();

			try
			{
				if (m_Systems.ContainsKey(id))
				{
					throw new ArgumentException("id", string.Format("{0} already contains {1} with id {2}",
					                                                GetType().Name, typeof(CrosspointSystem).Name, id));
				}

				CrosspointSystem output = new CrosspointSystem(id);
				m_Systems[id] = output;

				return output;
			}
			finally
			{
				m_SystemsSection.Leave();
			}
		}

		/// <summary>
		/// Gets the system with the given ID
		/// If no system exists, a new one is added and returned
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public CrosspointSystem GetOrCreateSystem(int id)
		{
			m_SystemsSection.Enter();

			try
			{
				return m_Systems.ContainsKey(id)
					       ? GetSystem(id)
					       : CreateSystem(id);
			}
			finally
			{
				m_SystemsSection.Leave();
			}
		}

		/// <summary>
		/// Disposes and removes the system with the given id.
		/// </summary>
		/// <param name="id"></param>
		public bool RemoveSystem(int id)
		{
			m_SystemsSection.Enter();

			try
			{
				CrosspointSystem system;
				if (!m_Systems.TryGetValue(id, out system))
					return false;

				system.Dispose();
				return m_Systems.Remove(id);
			}
			finally
			{
				m_SystemsSection.Leave();
			}
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			CrosspointSystem[] systems = m_SystemsSection.Execute(() => m_Systems.Values.ToArray());

			yield return ConsoleNodeGroup.KeyNodeMap("systems", systems, s => (uint)s.Id);
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			string systems = m_SystemsSection.Execute(() => StringUtils.ArrayFormat(m_Systems.Keys.Order()));
			addRow("Systems", systems);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new GenericConsoleCommand<int>("CreateSystem", "Creates a new crosspoint system", id => CreateSystem(id));
			yield return new GenericConsoleCommand<int>("RemoveSystem", "Removes an existing crosspoint system", id => RemoveSystem(id));
		}

		#endregion
	}
}
