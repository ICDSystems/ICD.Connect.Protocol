namespace ICD.Connect.Protocol.Ports.IoPort
{
	public interface IIoPortSettings : IPortSettings
	{
		/// <summary>
		/// Gets/sets the port configuration.
		/// </summary>
		eIoPortConfiguration Configuration { get; set; }
	}
}
