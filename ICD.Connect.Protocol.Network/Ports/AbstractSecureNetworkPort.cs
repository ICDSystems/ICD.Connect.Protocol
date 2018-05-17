using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Protocol.Network.Ports
{
	public abstract class AbstractSecureNetworkPort<TSettings> : AbstractNetworkPort<TSettings>, ISecureNetworkPort
		where TSettings : ISecureNetworkPortSettings, new()
	{
		/// <summary>
		/// Gets/sets the username for authentication with the remote server.
		/// </summary>
		public abstract string Username { get; set; }

		/// <summary>
		/// Gets/sets the password for authentication with the remote server.
		/// </summary>
		public abstract string Password { get; set; }

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Username", Username);
			addRow("Password", StringUtils.PasswordFormat(Password));
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<string>("SetUsername",
														   "Sets the username for next connection attempt",
														   s => Username = s);
			yield return new GenericConsoleCommand<string>("SetPassword",
														   "Sets the password for next connection attempt",
														   s => Password = s);
		}

		/// <summary>
		/// Workaround to avoid "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
