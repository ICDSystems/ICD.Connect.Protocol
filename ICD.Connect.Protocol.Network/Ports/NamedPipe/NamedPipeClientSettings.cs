#if !SIMPLSHARP
using System.IO.Pipes;
using System.Security.Principal;
using ICD.Common.Utils.Xml;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Protocol.Network.Ports.NamedPipe
{
	[KrangSettings("NamedPipe", typeof(NamedPipeClient))]
	public sealed class NamedPipeClientSettings : AbstractSerialPortSettings, INamedPipeSettings
	{
		private readonly NamedPipeProperties m_NamedPipeProperties;

		#region Properties

		/// <summary>
		/// Gets/sets the configurable remote hostname.
		/// </summary>
		public string NamedPipeHostname
		{
			get { return m_NamedPipeProperties.NamedPipeHostname; }
			set { m_NamedPipeProperties.NamedPipeHostname = value; }
		}

		/// <summary>
		/// Gets/sets the configurable pipe name.
		/// </summary>
		public string NamedPipeName
		{
			get { return m_NamedPipeProperties.NamedPipeName; }
			set { m_NamedPipeProperties.NamedPipeName = value; }
		}

		/// <summary>
		/// Gets/sets the configurable pipe direction.
		/// </summary>
		public PipeDirection? NamedPipeDirection
		{
			get { return m_NamedPipeProperties.NamedPipeDirection; }
			set { m_NamedPipeProperties.NamedPipeDirection = value; }
		}

		/// <summary>
		/// Gets/sets the configurable pipe options.
		/// </summary>
		public PipeOptions? NamedPipeOptions
		{
			get { return m_NamedPipeProperties.NamedPipeOptions; }
			set { m_NamedPipeProperties.NamedPipeOptions = value; }
		}

		/// <summary>
		/// Gets/sets the configurable token impersonation level.
		/// </summary>
		public TokenImpersonationLevel? NamedPipeTokenImpersonationLevel
		{
			get { return m_NamedPipeProperties.NamedPipeTokenImpersonationLevel; }
			set { m_NamedPipeProperties.NamedPipeTokenImpersonationLevel = value; }
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public NamedPipeClientSettings()
		{
			m_NamedPipeProperties = new NamedPipeProperties();
		}

		/// <summary>
		/// Clears the configured values.
		/// </summary>
		public void ClearNamedPipeProperties()
		{
			m_NamedPipeProperties.ClearNamedPipeProperties();
		}

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			m_NamedPipeProperties.WriteElements(writer);
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			m_NamedPipeProperties.ParseXml(xml);
		}
	}
}
#endif
