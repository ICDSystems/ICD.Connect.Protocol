namespace ICD.Connect.Protocol.Crosspoints.Crosspoints
{
	public enum eCrosspointStatus
	{
		Uninitialized = 0,
		Idle = 1,
		Connected = 2,
		ControlNotFound = 3,
		EquipmentNotFound = 4,
		ConnectFailed = 5,
		ConnectionDropped = 6,
		ConnectionClosedRemote = 7,
	}
}
