using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Settings;

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
		public abstract ISecureNetworkProperties SecureNetworkProperties { get; }

		/// <summary>
		/// Gets the Network configuration properties.
		/// </summary>
		public sealed override INetworkProperties NetworkProperties { get { return SecureNetworkProperties; } }

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
		/// Applies the secure network configuration to the port.
		/// </summary>
		public override void ApplyConfiguration()
		{
			ApplyConfiguration(SecureNetworkProperties);
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

			if (properties.NetworkUsername != null)
				Username = properties.NetworkUsername;

			if (properties.NetworkPassword != null)
				Password = properties.NetworkPassword;
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			SecureNetworkProperties.Clear();
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(TSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Copy(SecureNetworkProperties);
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(TSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			SecureNetworkProperties.Copy(settings);
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
