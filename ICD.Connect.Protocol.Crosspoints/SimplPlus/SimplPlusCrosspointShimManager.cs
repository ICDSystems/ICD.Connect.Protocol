using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Crosspoints.CrosspointManagers;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using ICD.Connect.Protocol.Crosspoints.SimplPlus.CrosspointShims;

namespace ICD.Connect.Protocol.Crosspoints.SimplPlus
{
	public sealed class SimplPlusCrosspointShimManager : IConsoleNode
	{
		private readonly List<ISimplPlusCrosspointShim> m_CrosspointShims;

		private readonly SafeCriticalSection m_CrosspointShimsCriticalSection;

		public void RegisterSPlusCrosspointShim(ISimplPlusCrosspointShim crosspointShim)
		{
			m_CrosspointShimsCriticalSection.Execute(() => m_CrosspointShims.Add(crosspointShim));
		}

		internal SimplPlusCrosspointShimManager()
		{
			m_CrosspointShims = new List<ISimplPlusCrosspointShim>();
			m_CrosspointShimsCriticalSection = new SafeCriticalSection();
		}

		public void RegisterXp3(Xp3 xp3)
		{
			xp3.OnSystemCreated += Xp3OnSystemCreated;
			xp3.OnSystemRemoved += Xp3OnSystemRemoved;
		}

		public void UnregisterXp3(Xp3 xp3)
		{
			xp3.OnSystemCreated -= Xp3OnSystemCreated;
			xp3.OnSystemRemoved -= Xp3OnSystemRemoved;
		}

		private void Xp3OnSystemCreated(object sender, CrosspointSystemEventArgs crosspointSystemEventArgs)
		{
			crosspointSystemEventArgs.System.OnControlCrosspointManagerCreated += SystemOnControlCrosspointManagerCreated;
			crosspointSystemEventArgs.System.OnEquipmentCrosspointManagerCreated += SystemOnEquipmentCrosspointManagerCreated;
		}

		private void Xp3OnSystemRemoved(object sender, CrosspointSystemEventArgs crosspointSystemEventArgs)
		{
			crosspointSystemEventArgs.System.OnControlCrosspointManagerCreated -= SystemOnControlCrosspointManagerCreated;
			crosspointSystemEventArgs.System.OnEquipmentCrosspointManagerCreated -= SystemOnEquipmentCrosspointManagerCreated;
		}

		private void SystemOnEquipmentCrosspointManagerCreated(object sender, EventArgs eventArgs)
		{
			CrosspointSystem system = sender as CrosspointSystem;

			if (system == null)
				return;

			EquipmentCrosspointManager equipmentCrosspointManager = system.EquipmentCrosspointManager;
			if (equipmentCrosspointManager == null)
				return;

			equipmentCrosspointManager.OnCrosspointRegistered += EquipmentCrosspointManagerOnCrosspointRegistered;
			equipmentCrosspointManager.OnCrosspointUnregistered += EquipmentCrosspointManagerOnCrosspointUnregistered;
		}

		private void SystemOnControlCrosspointManagerCreated(object sender, EventArgs eventArgs)
		{
			CrosspointSystem system = sender as CrosspointSystem;

			if (system == null)
				return;

			ControlCrosspointManager controlCrosspointManager = system.ControlCrosspointManager;
			if (controlCrosspointManager == null)
				return;

			controlCrosspointManager.OnCrosspointRegistered += ControlCrosspointManagerOnCrosspointRegistered;
			controlCrosspointManager.OnCrosspointUnregistered += ControlCrosspointManagerOnCrosspointUnregistered;
		}

		/// <summary>
		/// todo: implement method
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="crosspoint"></param>
		private void EquipmentCrosspointManagerOnCrosspointUnregistered(ICrosspointManager sender, ICrosspoint crosspoint)
		{
			//throw new NotImplementedException();
		}

		/// <summary>
		/// todo: implement method
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="crosspoint"></param>
		private void EquipmentCrosspointManagerOnCrosspointRegistered(ICrosspointManager sender, ICrosspoint crosspoint)
		{
			//throw new NotImplementedException();
		}

		/// <summary>
		/// todo: implement method
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="crosspoint"></param>
		private void ControlCrosspointManagerOnCrosspointUnregistered(ICrosspointManager sender, ICrosspoint crosspoint)
		{
			//throw new NotImplementedException();
		}

		/// <summary>
		/// todo: implement method
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="crosspoint"></param>
		private void ControlCrosspointManagerOnCrosspointRegistered(ICrosspointManager sender, ICrosspoint crosspoint)
		{
			//throw new NotImplementedException();
		}

		#region Console

		public string ConsoleName { get { return "Xp3SimplPlus"; } }
		public string ConsoleHelp { get { return "The SimplPlus Shims around XP3 Crosspoints"; } }

		public IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield break;
		}

		public void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("Shim Count", m_CrosspointShims.Count);
		}

		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new ConsoleCommand("Print", "Prints the list of crosspoint shims", () => PrintShims());
			yield return
				new GenericConsoleCommand<int>("GetInfo", "Gets info about the specified shim", index => PrintShim(index));
		}

		/// <summary>
		/// Prints the table of active connections to the console.
		/// </summary>
		private void PrintShims()
		{
			TableBuilder builder = new TableBuilder("Index", "Simpl Instance", "CrosspointType", "CrosspointName", "CrosspointID");

			m_CrosspointShimsCriticalSection.Enter();

			try
			{
				m_CrosspointShims.ForEach(
				                             (item, index) =>
				                             builder.AddRow(index, item.CrosspointSymbolInstanceName,
				                                            item.Crosspoint != null ? item.Crosspoint.GetType().ToString() : "",
				                                            item.Crosspoint != null ? item.Crosspoint.Name : "",
				                                            item.Crosspoint != null ? item.Crosspoint.Id.ToString() : ""));
			}
			finally
			{
				m_CrosspointShimsCriticalSection.Leave();
			}

			IcdConsole.ConsoleCommandResponseLine(builder.ToString());
		}

		private void PrintShim(int index)
		{
			m_CrosspointShimsCriticalSection.Enter();

			try
			{
				IcdConsole.ConsoleCommandResponseLine(m_CrosspointShims.Count > index
					                                      ? m_CrosspointShims[index].GetShimInfo()
					                                      : "Invalid Index");
			}
			finally
			{
				m_CrosspointShimsCriticalSection.Leave();
			}
		}

		#endregion
	}
}
