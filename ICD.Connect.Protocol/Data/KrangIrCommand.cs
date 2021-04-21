using System.Collections.Generic;

namespace ICD.Connect.Protocol.Data
{
	public sealed class KrangIrCommand
	{
		public string Name { get; set; }
		public int Frequency { get; set; }
		public int RepeatCount { get; set; }
		public List<int> Data { get; set; }
	}
}