﻿using System.Collections.Generic;
using System.Linq;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Protocol.Extensions
{
	public sealed class CorePortCollection : AbstractOriginatorCollection<IPort>
	{
		public CorePortCollection() : base()
		{
		}

		public CorePortCollection(IEnumerable<IPort> children) : base(children)
		{
		}
	}

	public static class CoreExtensions
	{
		public static CorePortCollection GetPorts(this ICore core)
		{
			return new CorePortCollection(core.Originators.OfType<IPort>());
		}
	}
}