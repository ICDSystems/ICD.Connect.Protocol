using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace ICD.Connect.Protocol.Crosspoints
{
	public class CrosspointSystemEventArgs:EventArgs
	{

		public CrosspointSystem System { get; private set; }

		public CrosspointSystemEventArgs(CrosspointSystem system)
		{
			System = system;
		}
	}
}