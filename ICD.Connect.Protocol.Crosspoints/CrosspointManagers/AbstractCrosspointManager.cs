﻿using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.Crosspoints.CrosspointManagers
{
	/// <summary>
	/// Crosspoint managers are responsible for a collection of child crosspoints.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class AbstractCrosspointManager<T> : ICrosspointManager<T>
		where T : class, ICrosspoint
	{
		#region Events

		/// <summary>
		/// Raised when a crosspoint is registered with the manager.
		/// </summary>
		public event CrosspointManagerCrosspointCallback OnCrosspointRegistered;

		/// <summary>
		/// Raised when a crosspoint is unregistered from the manager.
		/// </summary>
		public event CrosspointManagerCrosspointCallback OnCrosspointUnregistered;

		#endregion

		#region Private Members

		private readonly IcdSortedDictionary<int, T> m_Crosspoints;
		private readonly SafeCriticalSection m_CrosspointsSection;
		private readonly RemoteCrosspointTracker m_RemoteCrosspoints;
		private readonly int m_SystemId;

		#endregion

		#region Public Properties

		/// <summary>
		/// When enabled the crosspoint manager will print timing information for messages.
		/// </summary>
		public bool Debug { get; set; }

		/// <summary>
		/// Gets the id of the parent system.
		/// </summary>
		public int SystemId { get { return m_SystemId; } }

		/// <summary>
		/// Gets the remote crosspoints for this manager. E.g. if this is a ControlCrosspointManager,
		/// this collection contains discovered equipment crosspoint info.
		/// </summary>
		public RemoteCrosspointTracker RemoteCrosspoints { get { return m_RemoteCrosspoints; } }

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public string ConsoleName { get { return GetType().GetNameWithoutGenericArity(); } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public abstract string ConsoleHelp { get; }

		protected ILoggerService Logger { get { return ServiceProvider.GetService<ILoggerService>(); } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="systemId">The id of the parent system.</param>
		protected AbstractCrosspointManager(int systemId)
		{
			m_Crosspoints = new IcdSortedDictionary<int, T>();
			m_CrosspointsSection = new SafeCriticalSection();

			m_RemoteCrosspoints = new RemoteCrosspointTracker();
			Subscribe(m_RemoteCrosspoints);

			m_SystemId = systemId;
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public virtual void Dispose()
		{
			OnCrosspointRegistered = null;
			OnCrosspointUnregistered = null;

			Unsubscribe(m_RemoteCrosspoints);
			m_RemoteCrosspoints.Dispose();

			ClearCrosspoints();
		}

		#region Public Methods

		/// <summary>
		/// Unregisters all of the registered crosspoints.
		/// </summary>
		[PublicAPI]
		public void ClearCrosspoints()
		{
			T[] values = GetCrosspoints().ToArray();
			foreach (T crosspoint in values)
				UnregisterCrosspoint(crosspoint);
		}

		/// <summary>
		/// Gets the available crosspoint ids.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public IEnumerable<int> GetCrosspointIds()
		{
			return m_CrosspointsSection.Execute(() => m_Crosspoints.Keys.ToArray());
		}

		/// <summary>
		/// Gets the available crosspoints.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public IEnumerable<T> GetCrosspoints()
		{
			return m_CrosspointsSection.Execute(() => m_Crosspoints.Values.ToArray());
		}
		IEnumerable<ICrosspoint> ICrosspointManager.GetCrosspoints()
		{
			return GetCrosspoints().Cast<ICrosspoint>();
		}

		/// <summary>
		/// Gets the crosspoint with the given id.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		[PublicAPI]
		public T GetCrosspoint(int id)
		{
			T output;
			if (TryGetCrosspoint(id, out output))
				return output;

			string message = string.Format("{0} does not contain {1} with id {2}", GetType().Name, typeof(T).Name, id);
			throw new KeyNotFoundException(message);
		}

		/// <summary>
		/// Gets the crosspoint with the given id.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="crosspoint"></param>
		/// <returns></returns>
		[PublicAPI]
		public bool TryGetCrosspoint(int id, out T crosspoint)
		{
			m_CrosspointsSection.Enter();

			try
			{
				return m_Crosspoints.TryGetValue(id, out crosspoint);
			}
			finally
			{
				m_CrosspointsSection.Leave();
			}
		}

		/// <summary>
		/// Creates a default crosspoint and adds it to the manager.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		ICrosspoint ICrosspointManager.CreateCrosspoint(int id, string name)
		{
			return CreateCrosspoint(id, name);
		}

		/// <summary>
		/// Creates a default crosspoint and adds it to the manager.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public T CreateCrosspoint(int id, string name)
		{
			m_CrosspointsSection.Enter();

			try
			{
				if (m_Crosspoints.ContainsKey(id))
					throw new ArgumentOutOfRangeException("id", "Already contains a crosspoint with the given id");

				T crosspoint = InstantiateCrosspoint(id, name);
				RegisterCrosspoint(crosspoint);

				return crosspoint;
			}
			finally
			{
				m_CrosspointsSection.Leave();
			}
		}

		/// <summary>
		/// Instantiates a new crosspoint with the given id and name.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		protected abstract T InstantiateCrosspoint(int id, string name);

		/// <summary>
		/// Adds the crosspoint to the manager.
		/// </summary>
		/// <param name="crosspoint"></param>
		[PublicAPI]
		public void RegisterCrosspoint(T crosspoint)
		{
			if (crosspoint == null)
				throw new ArgumentNullException("crosspoint");

			m_CrosspointsSection.Enter();

			try
			{
				if (m_Crosspoints.ContainsKey(crosspoint.Id))
					throw new ArgumentException("id", string.Format("{0} with id {1} already exists", typeof(T).Name, crosspoint.Id));

				m_Crosspoints[crosspoint.Id] = crosspoint;
				Subscribe(crosspoint);
			}
			finally
			{
				m_CrosspointsSection.Leave();
			}

			CrosspointManagerCrosspointCallback handler = OnCrosspointRegistered;
			if (handler != null)
				handler(this, crosspoint);
		}
		void ICrosspointManager.RegisterCrosspoint(ICrosspoint crosspoint)
		{
			RegisterCrosspoint((T)crosspoint);
		}

		/// <summary>
		/// Removes the crosspoint from the manager.
		/// </summary>
		/// <param name="crosspoint"></param>
		[PublicAPI]
		public void UnregisterCrosspoint(T crosspoint)
		{
			if (crosspoint == null)
				return;

			m_CrosspointsSection.Enter();

			try
			{
				Unsubscribe(crosspoint);

				if (!m_Crosspoints.Remove(crosspoint.Id))
					return;
			}
			finally
			{
				m_CrosspointsSection.Leave();
			}

			CrosspointManagerCrosspointCallback handler = OnCrosspointUnregistered;
			if (handler != null)
				handler(this, crosspoint);
		}
		void ICrosspointManager.UnregisterCrosspoint(ICrosspoint crosspoint)
		{
			UnregisterCrosspoint((T)crosspoint);
		}

		/// <summary>
		/// Gets the address of the crosspoint manager.
		/// </summary>
		/// <returns></returns>
		public HostInfo GetHostInfo()
		{
			string address = IcdEnvironment.NetworkAddresses.FirstOrDefault();
			ushort port = Xp3Utils.GetPortForSystem(m_SystemId);

			return new HostInfo(address, port);
		}

		public override string ToString()
		{
			return new ReprBuilder(this).AppendProperty("SystemId", SystemId).ToString();
		}

		#endregion

		#region Remote Crosspoint Callbacks

		/// <summary>
		/// Subscribe to the RemoteCrosspointTracker events.
		/// </summary>
		/// <param name="remoteCrosspoints"></param>
		private void Subscribe(RemoteCrosspointTracker remoteCrosspoints)
		{
			remoteCrosspoints.OnCrosspointDiscovered += RemoteCrosspointsOnCrosspointDiscovered;
			remoteCrosspoints.OnCrosspointLost += RemoteCrosspointsOnCrosspointLost;
		}

		/// <summary>
		/// Unsubscribe from the RemoteCrosspointTracker events.
		/// </summary>
		/// <param name="remoteCrosspoints"></param>
		private void Unsubscribe(RemoteCrosspointTracker remoteCrosspoints)
		{
			remoteCrosspoints.OnCrosspointDiscovered -= RemoteCrosspointsOnCrosspointDiscovered;
			remoteCrosspoints.OnCrosspointLost -= RemoteCrosspointsOnCrosspointLost;
		}

		/// <summary>
		/// Called when a remote crosspoint is discovered.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="crosspointInfo"></param>
		private void RemoteCrosspointsOnCrosspointDiscovered(RemoteCrosspointTracker sender, CrosspointInfo crosspointInfo)
		{
			Logger.AddEntry(eSeverity.Informational, "{0} - Discovered remote crosspoint {1}", this, crosspointInfo);
		}

		/// <summary>
		/// Called when a remote crosspoint is lost.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="crosspointInfo"></param>
		private void RemoteCrosspointsOnCrosspointLost(RemoteCrosspointTracker sender, CrosspointInfo crosspointInfo)
		{
			Logger.AddEntry(eSeverity.Warning, "{0} - Lost remote crosspoint {1}", this, crosspointInfo);
		}

		#endregion

		#region Crosspoint Callbacks

		/// <summary>
		/// Subscribe to the crosspoint events.
		/// </summary>
		/// <param name="crosspoint"></param>
		protected virtual void Subscribe(T crosspoint)
		{
			if (crosspoint == null)
				return;

			crosspoint.OnSendInputData += CrosspointOnSendInputData;
		}

		/// <summary>
		/// Unsubscribe from the crosspoint events.
		/// </summary>
		/// <param name="crosspoint"></param>
		protected virtual void Unsubscribe(T crosspoint)
		{
			if (crosspoint == null)
				return;

			crosspoint.OnSendInputData -= CrosspointOnSendInputData;
		}

		/// <summary>
		/// Called when the crosspoint raises data to be sent over the network.
		/// </summary>
		/// <param name="crosspoint"></param>
		/// <param name="data"></param>
		private void CrosspointOnSendInputData(ICrosspoint crosspoint, CrosspointData data)
		{
			if (Debug)
                IcdConsole.PrintLine("{0} - Receiving {1} from {2}", IcdEnvironment.GetLocalTime().ToLongTimeStringWithMilliseconds(), data, crosspoint);

			CrosspointOnSendInputData(crosspoint as T, data);
		}

		/// <summary>
		/// Sends data from the crosspoint to the program.
		/// </summary>
		/// <param name="crosspoint"></param>
		/// <param name="data"></param>
		protected abstract void CrosspointOnSendInputData(T crosspoint, CrosspointData data);

		/// <summary>
		/// Sends data from the program to the crosspoint.
		/// </summary>
		/// <param name="crosspoint"></param>
		/// <param name="data"></param>
		protected virtual void SendCrosspointOutputData(T crosspoint, CrosspointData data)
		{
			if (Debug)
                IcdConsole.PrintLine("{0} - Sending {1} to {2}", IcdEnvironment.GetLocalTime().ToLongTimeStringWithMilliseconds(), data, crosspoint);

			crosspoint.SendOutputData(data);
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public virtual IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield return ConsoleNodeGroup.KeyNodeMap("Crosspoints", "The crosspoints registered with this manager", GetCrosspoints(), c => (uint)c.Id);
			yield return RemoteCrosspoints;
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public virtual void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			int crosspointCount = m_CrosspointsSection.Execute(() => m_Crosspoints.Count);

			addRow("System Id", m_SystemId);
			addRow("Registered Crosspoints", crosspointCount);
			addRow("Remote Crosspoints", RemoteCrosspoints.Count);
			addRow("Debug", Debug);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public virtual IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new ConsoleCommand("ToggleDebug", "When enabled prints timing information.", () => Debug = !Debug);
			yield return
				new GenericConsoleCommand<int, string>("CreateCrosspoint", "CreateCrosspoint <ID> <NAME>",
				                                       (i, s) => CreateCrosspoint(i, s));
		}

		#endregion
	}
}
