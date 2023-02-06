using System.Collections.Generic;

namespace ICD.Connect.Protocol.Data
{
	public sealed class IrCommand
	{
		public string Name { get; set; }
		public int Frequency { get; set; }
		public int RepeatCount { get; set; }
		public int Offset { get; set; }
		public List<int> Data { get; set; }
	}
}