using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;

namespace ICD.Connect.Protocol.Data
{
	public sealed class KrangIrDriver
	{
		#region Members

		private readonly SafeCriticalSection m_CommandsSection;
		private readonly List<KrangIrCommand> m_Commands;

		#endregion

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		public KrangIrDriver()
		{
			m_CommandsSection = new SafeCriticalSection();
			m_Commands = new List<KrangIrCommand>();
		}

		#endregion

		#region Methods

		public IEnumerable<KrangIrCommand> GetCommands()
		{
			return m_CommandsSection.Execute(() => m_Commands.ToArray());
		}

		[CanBeNull]
		public KrangIrCommand GetCommandFromName(string name)
		{
			m_CommandsSection.Enter();

			try
			{
				return m_Commands.FirstOrDefault(c => c.Name == name);
			}
			finally
			{
				m_CommandsSection.Leave();
			}
		}

		public void AddCommand([NotNull] KrangIrCommand command)
		{
			if (command == null)
				throw new ArgumentNullException("command");

			m_CommandsSection.Execute(() => m_Commands.Add(command));
		}

		#endregion
	}
}