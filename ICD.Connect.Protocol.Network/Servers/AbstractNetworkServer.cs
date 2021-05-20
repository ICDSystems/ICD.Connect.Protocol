using System;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.Network.Servers
{
	public abstract class AbstractNetworkServer : AbstractServer<HostInfo>, INetworkServer
	{
		private const string ACCEPT_ALL = "0.0.0.0";

		#region Properties

		/// <summary>
		/// IP Address to accept connection from.
		/// </summary>
		public string AddressToAcceptConnectionFrom { get; set; }

		/// <summary>
		/// Port for server to listen on.
		/// </summary>
		public ushort Port { get; set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractNetworkServer()
		{
			AddressToAcceptConnectionFrom = ACCEPT_ALL;
		}

		#region Methods

		/// <summary>
		/// Override to add additional properties to the ToString representation.
		/// </summary>
		/// <param name="addPropertyAndValue"></param>
		protected override void BuildStringRepresentationProperties(Action<string, object> addPropertyAndValue)
		{
			base.BuildStringRepresentationProperties(addPropertyAndValue);

			addPropertyAndValue("Port", Port);
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

			addRow("Port", Port);
		}

		#endregion
	}
}