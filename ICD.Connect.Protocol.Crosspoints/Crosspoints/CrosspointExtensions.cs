using System;
using ICD.Common.Properties;

namespace ICD.Connect.Protocol.Crosspoints.Crosspoints
{
	public static class CrosspointExtensions
	{
		/// <summary>
		/// Shorthand for sending a single sig value to XP3.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="number"></param>
		/// <param name="value"></param>
		[PublicAPI]
		public static void SendInputSig([NotNull] this ICrosspoint extends, uint number, bool value)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			extends.SendInputSig(0, number, value);
		}

		/// <summary>
		/// Shorthand for sending a single sig value to XP3.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="smartObject"></param>
		/// <param name="number"></param>
		/// <param name="value"></param>
		[PublicAPI]
		public static void SendInputSig([NotNull] this ICrosspoint extends, ushort smartObject, uint number, bool value)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			CrosspointData data = new CrosspointData();
			data.AddSig(smartObject, number, value);
			extends.SendInputData(data);
		}

		/// <summary>
		/// Shorthand for sending a single sig value to XP3.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="number"></param>
		/// <param name="value"></param>
		[PublicAPI]
		public static void SendInputSig([NotNull] this ICrosspoint extends, uint number, ushort value)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");
			
			extends.SendInputSig(0, number, value);
		}

		/// <summary>
		/// Shorthand for sending a single sig value to XP3.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="smartObject"></param>
		/// <param name="number"></param>
		/// <param name="value"></param>
		[PublicAPI]
		public static void SendInputSig([NotNull] this ICrosspoint extends, ushort smartObject, uint number, ushort value)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			CrosspointData data = new CrosspointData();
			data.AddSig(smartObject, number, value);
			extends.SendInputData(data);
		}

		/// <summary>
		/// Shorthand for sending a single sig value to XP3.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="number"></param>
		/// <param name="value"></param>
		[PublicAPI]
		public static void SendInputSig([NotNull] this ICrosspoint extends, uint number, string value)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			extends.SendInputSig(0, number, value);
		}

		/// <summary>
		/// Shorthand for sending a single sig value to XP3.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="smartObject"></param>
		/// <param name="number"></param>
		/// <param name="value"></param>
		[PublicAPI]
		public static void SendInputSig([NotNull] this ICrosspoint extends, ushort smartObject, uint number, string value)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			CrosspointData data = new CrosspointData();
			data.AddSig(smartObject, number, value);
			extends.SendInputData(data);
		}
	}
}