using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Network.Settings;

namespace ICD.Connect.Protocol.Network.Ports
{
	public abstract class AbstractSecureNetworkPort<TSettings> : AbstractNetworkPort<TSettings>, ISecureNetworkPort
		where TSettings : ISecureNetworkPortSettings, new()
	{
		#region Properties

		/// <summary>
		/// Gets/sets the username for authentication with the remote server.
		/// </summary>
		public abstract string Username { get; set; }

		/// <summary>
		/// Gets/sets the password for authentication with the remote server.
		/// </summary>
		public abstract string Password { get; set; }

		/// <summary>
		/// Gets the Secure Network configuration properties.
		/// </summary>
		protected abstract SecureNetworkProperties SecureNetworkProperties { get; }

		/// <summary>
		/// Gets the Network configuration properties.
		/// </summary>
		protected override INetworkProperties NetworkProperties { get { return SecureNetworkProperties; } }

		#endregion

		#region Methods

		/// <summary>
		/// Applies the given device configuration properties to the port.
		/// </summary>
		/// <param name="properties"></param>
		public void ApplyDeviceConfiguration(ISecureNetworkProperties properties)
		{
			if (properties == null)
				throw new ArgumentNullException("properties");

			// Port supercedes device configuration
			ISecureNetworkProperties config = SecureNetworkProperties.Superimpose(properties);

			ApplyConfiguration(config);
		}

		/// <summary>
		/// Applies the given configuration properties to the port.
		/// </summary>
		/// <param name="properties"></param>
		public void ApplyConfiguration(ISecureNetworkProperties properties)
		{
			if (properties == null)
				throw new ArgumentNullException("properties");

			// Apply the network properties
			base.ApplyConfiguration(properties);

			Username = properties.NetworkUsername;
			Password = properties.NetworkPassword;
		}

		#endregion

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
			addRow("Address", Address);
			addRow("Port", Port);
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
			yield return new GenericConsoleCommand<string>("SetAddress",
														   "Sets the address for next connection attempt",
														   s => Address = s);
			yield return new GenericConsoleCommand<ushort>("SetPort",
														   "Sets the port for next connection attempt",
														   s => Port = s);
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
