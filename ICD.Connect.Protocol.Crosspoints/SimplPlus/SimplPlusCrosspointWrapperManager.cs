﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Crosspoints.SimplPlus.CrosspointWrappers;

namespace ICD.Connect.Protocol.Crosspoints.SimplPlus
{
	public class SimplPlusCrosspointWrapperManager : IConsoleNode
	{

		private readonly List<ISimplPlusCrosspointWrapper> m_CrosspointWrappers;

		private readonly SafeCriticalSection m_CrosspointWrapperssCriticalSection;

		public void RegisterSPlusCrosspointWrapper(ISimplPlusCrosspointWrapper crosspointWrapper)
		{
			m_CrosspointWrapperssCriticalSection.Execute(() => m_CrosspointWrappers.Add(crosspointWrapper));
		}

		internal SimplPlusCrosspointWrapperManager()
		{
			m_CrosspointWrappers = new List<ISimplPlusCrosspointWrapper>();
			m_CrosspointWrapperssCriticalSection = new SafeCriticalSection();

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