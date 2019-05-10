using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Crosspoints.Advertisements;
using ICD.Connect.Protocol.Crosspoints.CrosspointManagers;

namespace ICD.Connect.Protocol.Crosspoints
{
	/// <summary>
	/// The CrosspointSystem may contain one or more ControlCrosspointManagers and EquipmentCrosspointManagers.
	/// CrosspointSystems have an id, allowing for entirely separate crosspoint networks to exist in the
	/// same program.
	/// </summary>
	public sealed class CrosspointSystem : IDisposable, IConsoleNode
	{
		private readonly SafeCriticalSection m_CreateManagersSection;

		private readonly AdvertisementManager m_AdvertisementManager;
		private readonly int m_Id;

		private ControlCrosspointManager m_ControlCrosspointManager;
		private EquipmentCrosspointManager m_EquipmentCrosspointManager;

		#region Properties

		/// <summary>
		/// The ID for this crosspoint system.
		/// </summary>
		public int Id { get { return m_Id; } }

		/// <summary>
		/// The control crosspoints.
		/// </summary>
		[CanBeNull]
		public ControlCrosspointManager ControlCrosspointManager
		{
			get { return m_CreateManagersSection.Execute(() => m_ControlCrosspointManager); }
		}

		/// <summary>
		/// The equipment crosspoints.
		/// </summary>
		[CanBeNull]
		public EquipmentCrosspointManager EquipmentCrosspointManager
		{
			get { return m_CreateManagersSection.Execute(() => m_EquipmentCrosspointManager); }
		}

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public string ConsoleName { get { return GetType().Name; } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public string ConsoleHelp { get { return "Contains a Control manager and/or an Equipment manager."; } }

		/// <summary>
		/// Gets the crosspoint advertisement manager.
		/// </summary>
		public AdvertisementManager AdvertisementManager { get { return m_AdvertisementManager; } }

		#endregion

		#region Events

		public event EventHandler OnControlCrosspointManagerCreated;

		public event EventHandler OnEquipmentCrosspointManagerCreated;

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="id"></param>
		public CrosspointSystem(int id)
		{
			m_CreateManagersSection = new SafeCriticalSection();

			m_Id = id;

			m_AdvertisementManager = new AdvertisementManager(m_Id);
			m_AdvertisementManager.OnCrosspointsDiscovered += AdvertisementManagerOnCrosspointsDiscovered;
			m_AdvertisementManager.OnCrosspointsRemoved += AdvertisementManagerOnCrosspointsRemoved;
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			m_AdvertisementManager.OnCrosspointsDiscovered -= AdvertisementManagerOnCrosspointsDiscovered;
			m_AdvertisementManager.OnCrosspointsRemoved -= AdvertisementManagerOnCrosspointsRemoved;
			m_AdvertisementManager.Dispose();

			if (m_ControlCrosspointManager != null)
				m_ControlCrosspointManager.Dispose();
			m_ControlCrosspointManager = null;

			if (m_EquipmentCrosspointManager != null)
				m_EquipmentCrosspointManager.Dispose();
			m_EquipmentCrosspointManager = null;
		}

		/// <summary>
		/// Instantiates a ControlCrosspointManager.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException">Already contains a ControlCrosspointManager.</exception>
		public ControlCrosspointManager CreateControlCrosspointManager()
		{
			m_CreateManagersSection.Enter();

			try
			{
				if (m_ControlCrosspointManager != null)
				{
					throw new InvalidOperationException(string.Format("{0} {1} already contains a {2}", GetType().Name, m_Id,
					                                                  typeof(ControlCrosspointManager).Name));
				}

				m_ControlCrosspointManager = new ControlCrosspointManager(Id);
				m_AdvertisementManager.AdvertiseControlCrosspoints(m_ControlCrosspointManager);
			}
			finally
			{
				m_CreateManagersSection.Leave();
			}

			OnControlCrosspointManagerCreated.Raise(this);

			return m_ControlCrosspointManager;
		}

		/// <summary>
		/// Gets the ControlCrosspointManager, creates one if it doesn't already exist
		/// </summary>
		/// <returns></returns>
		public ControlCrosspointManager GetOrCreateControlCrosspointManager()
		{
			m_CreateManagersSection.Enter();

			try
			{
				return m_ControlCrosspointManager ?? CreateControlCrosspointManager();
			}
			finally
			{
				m_CreateManagersSection.Leave();
			}
		}

		/// <summary>
		/// Instantiates an EquipmentCrosspointManager.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException">Already contains a EquipmentCrosspointManager.</exception>
		public EquipmentCrosspointManager CreateEquipmentCrosspointManager()
		{
			m_CreateManagersSection.Enter();

			try
			{
				if (m_EquipmentCrosspointManager != null)
				{
					throw new InvalidOperationException(string.Format("{0} {1} already contains a {2}", GetType().Name, m_Id,
					                                                  typeof(EquipmentCrosspointManager).Name));
				}

				m_EquipmentCrosspointManager = new EquipmentCrosspointManager(Id);
				m_AdvertisementManager.AdvertiseEquipmentCrosspoints(m_EquipmentCrosspointManager);
			}
			finally
			{
				m_CreateManagersSection.Leave();
			}

			OnEquipmentCrosspointManagerCreated.Raise(this);

			return m_EquipmentCrosspointManager;
		}

		/// <summary>
		/// Gets the EquipmentCrosspointManager, creates one if it doesn't already exist
		/// </summary>
		/// <returns></returns>
		public EquipmentCrosspointManager GetOrCreateEquipmentCrosspointManager()
		{
			m_CreateManagersSection.Enter();

			try
			{
				return m_EquipmentCrosspointManager ?? CreateEquipmentCrosspointManager();
			}
			finally
			{
				m_CreateManagersSection.Leave();
			}
		}

		#endregion

		/// <summary>
		/// Called by the AdvertisementMananger when we discover new equipment/controls on the network.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void AdvertisementManagerOnCrosspointsDiscovered(object sender, AdvertisementEventArgs args)
		{
			m_CreateManagersSection.Enter();

			try
			{
				if (m_ControlCrosspointManager != null && args.Data.Equipment != null)
					m_ControlCrosspointManager.RemoteCrosspoints.AddCrosspointInfo(args.Data.Equipment);

				if (m_EquipmentCrosspointManager != null && args.Data.Controls != null)
					m_EquipmentCrosspointManager.RemoteCrosspoints.AddCrosspointInfo(args.Data.Controls);
			}
			finally
			{
				m_CreateManagersSection.Leave();
			}
		}

		/// <summary>
		/// Called by the AdvertisementMananger when we discover new equipment/controls on the network.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void AdvertisementManagerOnCrosspointsRemoved(object sender, AdvertisementEventArgs args)
		{
			m_CreateManagersSection.Enter();

			try
			{
				if (m_ControlCrosspointManager != null)
					m_ControlCrosspointManager.RemoteCrosspoints.RemoveCrosspointInfo(args.Data.Equipment);

				if (m_EquipmentCrosspointManager != null)
					m_EquipmentCrosspointManager.RemoteCrosspoints.RemoveCrosspointInfo(args.Data.Controls);
			}
			finally
			{
				m_CreateManagersSection.Leave();
			}
		}

		#region Console

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield return m_AdvertisementManager;

			if (ControlCrosspointManager != null)
				yield return ControlCrosspointManager;

			if (EquipmentCrosspointManager != null)
				yield return EquipmentCrosspointManager;
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("Id", m_Id);
			addRow("Has Control Crosspoint Manager", ControlCrosspointManager != null);
			addRow("Has Equipment Crosspoint Manager", EquipmentCrosspointManager != null);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new ConsoleCommand("CreateControlManager", "Creates a control crosspoint manager", () => CreateControlCrosspointManager());
			yield return new ConsoleCommand("CreateEquipmentManager", "Creates a control crosspoint manager", () => CreateEquipmentCrosspointManager());
		}

		#endregion
	}
}
