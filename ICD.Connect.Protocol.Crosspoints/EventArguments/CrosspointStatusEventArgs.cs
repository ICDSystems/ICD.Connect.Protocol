using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using ICD.Common.EventArguments;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;

namespace ICD.Connect.Protocol.Crosspoints.EventArguments
{
	public class CrosspointStatusEventArgs : GenericEventArgs<eCrosspointStatus>
	{
		public CrosspointStatusEventArgs(eCrosspointStatus data) : base(data)
		{
		}
	}
}