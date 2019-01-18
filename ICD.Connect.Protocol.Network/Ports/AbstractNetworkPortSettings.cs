using ICD.Common.Utils.Xml;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.Network.Ports
{
	public abstract class AbstractNetworkPortSettings : AbstractSerialPortSettings, INetworkPortSettings
	{
		#region Properties

		/// <summary>
		/// Gets/sets the configurable network address.
		/// </summary>
		public abstract string NetworkAddress { get; set; }

		/// <summary>
		/// Gets/sets the configurable network port.
		/// </summary>
		public abstract ushort? NetworkPort { get; set; }

		/// <summary>
		/// Clears the configured values.
		/// </summary>
		public abstract void ClearNetworkProperties();

		#endregion

		#region Methods

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			WriteNetworkElements(writer);
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			ParseNetworkElements(xml);
		}

		/// <summary>
		/// Override to serialize the network configuration to XML.
		/// </summary>
		/// <param name="writer"></param>
		protected abstract void WriteNetworkElements(IcdXmlTextWriter writer);

		/// <summary>
		/// Override to deserialize the network configuration from XML.
		/// </summary>
		/// <param name="xml"></param>
		protected abstract void ParseNetworkElements(string xml);

		#endregion
	}
}
