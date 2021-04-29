using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Protocol.Data
{
	public sealed class IrDriver
	{
		#region Members

		private readonly SafeCriticalSection m_CommandsSection;
		private readonly Dictionary<string, IrCommand> m_Commands;

		#endregion

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		public IrDriver()
		{
			m_CommandsSection = new SafeCriticalSection();
			m_Commands = new Dictionary<string, IrCommand>();
		}

		#endregion

		#region Methods

		public IEnumerable<IrCommand> GetCommands()
		{
			return m_CommandsSection.Execute(() => m_Commands.Values.ToArray());
		}

		[CanBeNull]
		public IrCommand GetCommandFromName(string name)
		{
			return m_CommandsSection.Execute(() => m_Commands.GetDefault(name));
		}

		public void AddCommand([NotNull] IrCommand command)
		{
			if (command == null)
				throw new ArgumentNullException("command");

			m_CommandsSection.Execute(() => m_Commands.Add(command.Name, command));
		}

		#endregion
	}
}