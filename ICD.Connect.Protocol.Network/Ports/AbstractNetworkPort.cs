using System;
using System.Collections.Generic;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.Network.Ports
{
	public abstract class AbstractNetworkPort<TSettings> : AbstractSerialPort<TSettings>, INetworkPort
		where TSettings : INetworkPortSettings, new()
	{
		#region Properties

		/// <summary>
		/// Gets/sets the hostname of the remote server.
		/// </summary>
		public abstract string Address { get; set; }

		/// <summary>
		/// Gets/sets the port of the remote server.
		/// </summary>
		public abstract ushort Port { get; set; }

		/// <summary>
		/// Gets the Network configuration properties.
		/// </summary>
		protected abstract INetworkProperties NetworkProperties { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Applies the given device configuration properties to the port.
		/// </summary>
		/// <param name="properties"></param>
		public void ApplyDeviceConfiguration(INetworkProperties properties)
		{
			if (properties == null)
				throw new ArgumentNullException("properties");

			// Port supercedes device configuration
			INetworkProperties config = NetworkProperties.Superimpose(properties);

			ApplyConfiguration(config);
		}

		/// <summary>
		/// Applies the given configuration properties to the port.
		/// </summary>
		/// <param name="properties"></param>
		public void ApplyConfiguration(INetworkProperties properties)
		{
			if (properties == null)
				throw new ArgumentNullException("properties");

			if (properties.NetworkAddress != null)
				Address = properties.NetworkAddress;

			if (properties.NetworkPort.HasValue)
				Port = properties.NetworkPort.Value;
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
