using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Connect.API;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Timers;
using ICD.Connect.Protocol.Crosspoints.Advertisements;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;

namespace ICD.Connect.Protocol.Crosspoints.CrosspointManagers
{
	/// <summary>
	/// Handles storage of crosspoints discovered via advertisement.
	/// Crosspoints are removed after an elapsed period of time.
	/// </summary>
	public sealed class RemoteCrosspointTracker : IDisposable, IConsoleNode
	{
		/// <summary>
		/// How often to check for elapsed crosspoint info.
		/// </summary>
		private const long ELAPSED_INTERVAL = AdvertisementManager.BROADCAST_INTERVAL;

		/// <summary>
		/// How old crosspoint info must be to be considered elapsed.
		/// </summary>
		private const long ELAPSED_THRESHOLD = AdvertisementManager.BROADCAST_INTERVAL * 5;

		private readonly Dictionary<int, CrosspointInfo> m_Crosspoints;
		private readonly Dictionary<int, DateTime> m_AddTimeMap;
		private readonly SafeCriticalSection m_CrosspointsSection;

		private readonly SafeTimer m_ElapsedTimer;

		#region Properties

		/// <summary>
		/// Gets the number of crosspoints in the lookup table.
		/// </summary>
		public int Count { get { return m_CrosspointsSection.Execute(() => m_Crosspoints.Count); } }

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public string ConsoleName { get { return "RemoteCrosspoints"; } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public string ConsoleHelp { get { return "Provides a mapping of crosspoint ids to host address."; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public RemoteCrosspointTracker()
		{
			m_Crosspoints = new Dictionary<int, CrosspointInfo>();
			m_AddTimeMap = new Dictionary<int, DateTime>();
			m_CrosspointsSection = new SafeCriticalSection();

			m_ElapsedTimer = new SafeTimer(CullElapsed, ELAPSED_INTERVAL, ELAPSED_INTERVAL);
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			m_ElapsedTimer.Dispose();
		}

		/// <summary>
		/// Adds the crosspoint to the crosspoint lookup table.
		/// </summary>
		/// <param name="crosspoint"></param>
		public void AddCrosspointInfo(CrosspointInfo crosspoint)
		{
			m_CrosspointsSection.Enter();

			try
			{
				// Warn if we got the same id from a different source
				CrosspointInfo old;
				if (m_Crosspoints.TryGetValue(crosspoint.Id, out old) && crosspoint.Host != old.Host)
				{
					IcdErrorLog.Warn("Discovered duplicate crosspoint with id {0}, possible id duplication? (old: {1}, new: {2})",
					                 crosspoint.Id, old, crosspoint);
				}

				m_Crosspoints[crosspoint.Id] = crosspoint;
				m_AddTimeMap[crosspoint.Id] = IcdEnvironment.GetLocalTime();
			}
			finally
			{
				m_CrosspointsSection.Leave();
			}
		}

		/// <summary>
		/// Adds the crosspoint to the crosspoint lookup table.
		/// </summary>
		/// <param name="crosspoints"></param>
		public void AddCrosspointInfo(IEnumerable<CrosspointInfo> crosspoints)
		{
			foreach (CrosspointInfo info in crosspoints)
				AddCrosspointInfo(info);
		}

		/// <summary>
		/// Removes the crosspoint from the crosspoint lookup table.
		/// </summary>
		/// <param name="id"></param>
		public void RemoveCrosspointInfo(int id)
		{
			m_CrosspointsSection.Enter();

			try
			{
				m_Crosspoints.Remove(id);
				m_AddTimeMap.Remove(id);
			}
			finally
			{
				m_CrosspointsSection.Leave();
			}
		}

		public void RemoveCrosspointInfo(IEnumerable<CrosspointInfo> crosspoints)
		{
			foreach (CrosspointInfo info in crosspoints)
				RemoveCrosspointInfo(info.Id);
		}

		/// <summary>
		/// Gets the crosspoints in the crosspoint lookup table.
		/// </summary>
		public IEnumerable<CrosspointInfo> GetCrosspointInfo()
		{
			return m_CrosspointsSection.Execute(() => m_Crosspoints.OrderValuesByKey().ToArray());
		}

		/// <summary>
		/// Gets the crosspoint in the crosspoint lookup table.
		/// </summary>
		/// <param name="id"></param>
		public CrosspointInfo GetCrosspointInfo(int id)
		{
			m_CrosspointsSection.Enter();

			try
			{
				if (m_Crosspoints.ContainsKey(id))
					return m_Crosspoints[id];

				string message = string.Format("{0} does not contain {1} with id {2}", GetType().Name, typeof(CrosspointInfo).Name,
				                               id);
				throw new KeyNotFoundException(message);
			}
			finally
			{
				m_CrosspointsSection.Leave();
			}
		}

		/// <summary>
		/// Attempts to retrieve info for the crosspoint with the given id.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="info"></param>
		/// <returns>True if the crosspoint exists</returns>
		public bool TryGetCrosspointInfo(int id, out CrosspointInfo info)
		{
			m_CrosspointsSection.Enter();

			try
			{
				return m_Crosspoints.TryGetValue(id, out info);
			}
			finally
			{
				m_CrosspointsSection.Leave();
			}
		}

		/// <summary>
		/// Returns true if the crosspoint exists in the crosspoint lookup table.
		/// </summary>
		/// <param name="id"></param>
		public bool ContainsCrosspointInfo(int id)
		{
			return m_CrosspointsSection.Execute(() => m_Crosspoints.ContainsKey(id));
		}

		#endregion

		/// <summary>
		/// Called periodically to cull elapsed crosspoint info.
		/// </summary>
		private void CullElapsed()
		{
			m_CrosspointsSection.Enter();

			try
			{
				foreach (int id in m_Crosspoints.Keys.ToArray())
				{
					DateTime added = m_AddTimeMap[id];
					TimeSpan age = IcdEnvironment.GetLocalTime() - added;

					if (age.TotalMilliseconds > ELAPSED_THRESHOLD)
						RemoveCrosspointInfo(id);
				}
			}
			finally
			{
				m_CrosspointsSection.Leave();
			}
		}

		#region Console

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield break;
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("Count", Count);
			addRow("Max Crosspoint Age (seconds)", ELAPSED_THRESHOLD / 1000.0f);
			addRow("Check for Expired Crosspoints (seconds)", ELAPSED_INTERVAL / 1000.0f);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new ConsoleCommand("PrintCrosspoints", "Prints the available crosspoint info", () => PrintCrosspoints());
		}

		/// <summary>
		/// Prints all of the remote crosspoint information.
		/// </summary>
		private void PrintCrosspoints()
		{
			TableBuilder builder = new TableBuilder("Id", "Name", "Host", "Last Seen");

			m_CrosspointsSection.Enter();

			try
			{
				foreach (int id in m_Crosspoints.Keys.Order())
				{
					CrosspointInfo info = m_Crosspoints[id];
					builder.AddRow(id, info.Name, info.Host, m_AddTimeMap[id]);
				}
			}
			finally
			{
				m_CrosspointsSection.Leave();
			}

			IcdConsole.ConsoleCommandResponse(builder.ToString());
		}

		#endregion
	}
}
