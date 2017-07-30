using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Crosspoints.CrosspointManagers;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using ICD.Connect.Protocol.Crosspoints.SimplPlus.CrosspointWrappers;

namespace ICD.Connect.Protocol.Crosspoints.SimplPlus
{
	public class SimplPlusCrosspointWrapperManager : IConsoleNode
	{

		private readonly List<ISimplPlusCrosspointWrapper> m_CrosspointWrappers;

		private readonly SafeCriticalSection m_CrosspointWrapperssCriticalSection;

		//private Dictionary<int, > 

		public void RegisterSPlusCrosspointWrapper(ISimplPlusCrosspointWrapper crosspointWrapper)
		{
			m_CrosspointWrapperssCriticalSection.Execute(() => m_CrosspointWrappers.Add(crosspointWrapper));
		}

		internal SimplPlusCrosspointWrapperManager()
		{
			m_CrosspointWrappers = new List<ISimplPlusCrosspointWrapper>();
			m_CrosspointWrapperssCriticalSection = new SafeCriticalSection();
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
			var system = sender as CrosspointSystem;

			if (system == null)
				return;

			system.EquipmentCrosspointManager.OnCrosspointRegistered += EquipmentCrosspointManagerOnCrosspointRegistered;
			system.EquipmentCrosspointManager.OnCrosspointUnregistered += EquipmentCrosspointManagerOnCrosspointUnregistered;
		}
		
		private void SystemOnControlCrosspointManagerCreated(object sender, EventArgs eventArgs)
		{
			var system = sender as CrosspointSystem;

			if (system == null)
				return;

			system.ControlCrosspointManager.OnCrosspointRegistered += ControlCrosspointManagerOnCrosspointRegistered;
			system.ControlCrosspointManager.OnCrosspointUnregistered += ControlCrosspointManagerOnCrosspointUnregistered;
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
		public string ConsoleHelp { get { return "The SimplPlus Wrappers around XP3 Crosspoints"; }}

		public IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield break;
		}

		public void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("Wrapper Count", m_CrosspointWrappers.Count);
		}

		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new ConsoleCommand("Print", "Prints the list of crosspoint wrappers/s", () => PrintWrappers());
			yield return
				new GenericConsoleCommand<int>("GetInfo", "Gets info about the specified wrapper", index => PrintWrapper(index));
		}

		/// <summary>
		/// Prints the table of active connections to the console.
		/// </summary>
		private void PrintWrappers()
		{
			TableBuilder builder = new TableBuilder("Index", "Simpl Instance", "CrosspointType", "CrosspointName", "CrosspointID");

			m_CrosspointWrapperssCriticalSection.Enter();

			try
			{
				m_CrosspointWrappers.ForEach(
				                             (item, index) =>
				                             builder.AddRow(index, item.CrosspointSymbolInstanceName,
				                                            item.Crosspoint != null ? item.Crosspoint.GetType().ToString() : "",
				                                            item.Crosspoint != null ? item.Crosspoint.Name : "",
															item.Crosspoint != null ? item.Crosspoint.Id.ToString() : ""));
			}
			finally
			{
				m_CrosspointWrapperssCriticalSection.Leave();
			}

			IcdConsole.ConsoleCommandResponseLine(builder.ToString());
		}

		private void PrintWrapper(int index)
		{
			m_CrosspointWrapperssCriticalSection.Enter();

			try
			{
				IcdConsole.ConsoleCommandResponseLine(m_CrosspointWrappers.Count > index
					                                      ? m_CrosspointWrappers[index].GetWrapperInfo()
					                                      : "Invalid Index");
			}
			finally
			{
				m_CrosspointWrapperssCriticalSection.Leave();
			}
		}

		#endregion

	}
}