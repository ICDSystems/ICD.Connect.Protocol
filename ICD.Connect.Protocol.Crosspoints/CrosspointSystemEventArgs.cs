using System;

namespace ICD.Connect.Protocol.Crosspoints
{
	public class CrosspointSystemEventArgs : EventArgs
	{
		public CrosspointSystem System { get; private set; }

		public CrosspointSystemEventArgs(CrosspointSystem system)
		{
			System = system;
		}
	}
}
