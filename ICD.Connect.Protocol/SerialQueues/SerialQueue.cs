using ICD.Common.Properties;

namespace ICD.Connect.Protocol.SerialQueues
{
	/// <summary>
	/// SerialQueue will delay subsequent commands until the previous command
	/// has received a response.
	/// </summary>
	[PublicAPI]
	public sealed class SerialQueue : AbstractSerialQueue
	{
	}
}
