using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Protocol.Crosspoints.Advertisements
{
	/// <summary>
	/// Used to notify the crosspoint system of the discovery of new controls or equipment.
	/// </summary>
	public sealed class AdvertisementEventArgs : GenericEventArgs<Advertisement>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="advertisement"></param>
		public AdvertisementEventArgs(Advertisement advertisement)
			: base(advertisement)
		{
		}
	}
}
