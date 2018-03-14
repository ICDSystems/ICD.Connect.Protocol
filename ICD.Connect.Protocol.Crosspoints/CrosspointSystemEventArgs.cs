using System;

namespace ICD.Connect.Protocol.Crosspoints
{
	public sealed class CrosspointSystemEventArgs : EventArgs
	{
		public CrosspointSystem System { get; private set; }

		public CrosspointSystemEventArgs(CrosspointSystem system)
		{
			System = system;
		}
	}
}
