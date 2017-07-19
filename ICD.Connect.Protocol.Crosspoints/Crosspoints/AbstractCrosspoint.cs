using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Connect.API;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Timers;
using ICD.Connect.Protocol.Crosspoints.EventArguments;
using ICD.Connect.Protocol.Sigs;
using Newtonsoft.Json;

namespace ICD.Connect.Protocol.Crosspoints.Crosspoints
{
	/// <summary>
	/// Base class for all crosspoints.
	/// </summary>
	public abstract class AbstractCrosspoint : ICrosspoint
	{
		private const double PING_TIMEOUT_SECONDS = 30;

		/// <summary>
		/// Raised when this crosspoint sends data to XP3.
		/// </summary>
		public event CrosspointDataReceived OnSendInputData;

		/// <summary>
		/// Raised when XP3 sends data to this crosspoint.
		/// </summary>
		public event CrosspointDataReceived OnSendOutputData;

		/// <summary>
		/// Raised when the status of this crosspoint changes.
		/// </summary>
		public event EventHandler<CrosspointStatusEventArgs> OnStatusChanged;

		private readonly Dictionary<string, DateTime> m_PingTimes;
		private readonly SafeCriticalSection m_PingTimesSection;

		private readonly int m_Id;
		private readonly string m_Name;

		private eCrosspointStatus m_Status;

		#region Properties

		/// <summary>
		/// The id for this crosspoint.
		/// </summary>
		public int Id { get { return m_Id; } }

		/// <summary>
		/// The human readable name of this crosspoint.
		/// </summary>
		public string Name { get { return m_Name; } }

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public string ConsoleName { get { return GetType().Name; } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public virtual string ConsoleHelp { get { return string.Empty; } }

		/// <summary>
		/// Gets or sets the status of the crosspoint
		/// </summary>
		public eCrosspointStatus Status
		{
			get { return m_Status; }
			internal set
			{
				if (m_Status == value)
					return;

				m_Status = value;
				OnStatusChanged.Raise(this, new CrosspointStatusEventArgs(m_Status));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="name"></param>
		protected AbstractCrosspoint(int id, string name)
		{
			if (id == Xp3Utils.NULL_EQUIPMENT)
			{
				string message = string.Format("Can not create {0} with id {1}", GetType().Name, id);
				throw new ArgumentException(message, "id");
			}

			m_PingTimes = new Dictionary<string, DateTime>();
			m_PingTimesSection = new SafeCriticalSection();

			m_Id = id;
			m_Name = name;
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public virtual void Dispose()
		{
			OnSendOutputData = null;
			OnSendInputData = null;
		}

		/// <summary>
		/// Called by XP3 to send data to this crosspoint.
		/// </summary>
		/// <param name="data"></param>
		[PublicAPI]
		public void SendOutputData(CrosspointData data)
		{
			if (data == null)
				throw new ArgumentNullException("data");

			PreSendOutputData(data);

			switch (data.MessageType)
			{
				case CrosspointData.eMessageType.Ping:
					ReceivePing(data);
					break;

				case CrosspointData.eMessageType.Pong:
					ReceivePong(data);
					break;
			}

			CrosspointDataReceived handler = OnSendOutputData;
			if (handler != null)
				handler(this, data);
		}

		/// <summary>
		/// Called by the program to send data to XP3.
		/// </summary>
		/// <param name="data"></param>
		[PublicAPI]
		public void SendInputData(CrosspointData data)
		{
			if (data == null)
				throw new ArgumentNullException("data");

			PreSendInputData(data);

			// Make sure the data is being sent to/from the correct place
			if (!data.GetControlIds().Any())
				data.AddControlIds(GetControlsForMessage());
			if (data.EquipmentId == 0)
				data.EquipmentId = GetEquipmentForMessage();

			CrosspointDataReceived handler = OnSendInputData;
			if (handler != null)
				handler(this, data);
		}

		/// <summary>
		/// Shorthand for sending a single sig value to XP3.
		/// </summary>
		/// <param name="number"></param>
		/// <param name="value"></param>
		[PublicAPI]
		public void SendInputSig(uint number, bool value)
		{
			SendInputSig(0, number, value);
		}

		/// <summary>
		/// Shorthand for sending a single sig value to XP3.
		/// </summary>
		/// <param name="smartObject"></param>
		/// <param name="number"></param>
		/// <param name="value"></param>
		[PublicAPI]
		public void SendInputSig(ushort smartObject, uint number, bool value)
		{
			CrosspointData data = new CrosspointData();
			data.AddSig(smartObject, number, value);
			SendInputData(data);
		}

		/// <summary>
		/// Shorthand for sending a single sig value to XP3.
		/// </summary>
		/// <param name="number"></param>
		/// <param name="value"></param>
		[PublicAPI]
		public void SendInputSig(uint number, ushort value)
		{
			SendInputSig(0, number, value);
		}

		/// <summary>
		/// Shorthand for sending a single sig value to XP3.
		/// </summary>
		/// <param name="smartObject"></param>
		/// <param name="number"></param>
		/// <param name="value"></param>
		[PublicAPI]
		public void SendInputSig(ushort smartObject, uint number, ushort value)
		{
			CrosspointData data = new CrosspointData();
			data.AddSig(smartObject, number, value);
			SendInputData(data);
		}

		/// <summary>
		/// Shorthand for sending a single sig value to XP3.
		/// </summary>
		/// <param name="number"></param>
		/// <param name="value"></param>
		[PublicAPI]
		public void SendInputSig(uint number, string value)
		{
			SendInputSig(0, number, value);
		}

		/// <summary>
		/// Shorthand for sending a single sig value to XP3.
		/// </summary>
		/// <param name="smartObject"></param>
		/// <param name="number"></param>
		/// <param name="value"></param>
		[PublicAPI]
		public void SendInputSig(ushort smartObject, uint number, string value)
		{
			CrosspointData data = new CrosspointData();
			data.AddSig(smartObject, number, value);
			SendInputData(data);
		}

		/// <summary>
		/// Gets the string representation for this crosspoint.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return string.Format("{0}(Id={1}, Name={2})", GetType().Name, Id, StringUtils.ToRepresentation(Name));
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets the source control or the destination controls for a message originating from this crosspoint.
		/// </summary>
		/// <returns></returns>
		protected abstract IEnumerable<int> GetControlsForMessage();

		/// <summary>
		/// Gets the source equipment or destination equipment for a message originating from this crosspoint.
		/// </summary>
		/// <returns></returns>
		protected abstract int GetEquipmentForMessage();

		/// <summary>
		/// Override this method to handle data before it is sent to XP3.
		/// </summary>
		/// <param name="data"></param>
		protected virtual void PreSendInputData(CrosspointData data)
		{
		}

		/// <summary>
		/// Override this method to handle data before it is sent to the program.
		/// </summary>
		/// <param name="data"></param>
		protected virtual void PreSendOutputData(CrosspointData data)
		{
		}

		#endregion

		#region Ping

		/// <summary>
		/// Sends a ping to the remote crosspoint/s.
		/// </summary>
		private void Ping()
		{
			ClearOldPingTimes();

			string key = Guid.NewGuid().ToString();
			m_PingTimesSection.Execute(() => m_PingTimes[key] = IcdEnvironment.GetLocalTime());

			int[] controls = GetControlsForMessage().ToArray();
			int equipment = GetEquipmentForMessage();

			IcdConsole.PrintLine("{0} {1} sending Ping - Controls={2}, Equipment={3}", GetType().Name, Id,
			                          StringUtils.ArrayFormat(controls), equipment);

			CrosspointData ping = CrosspointData.Ping(controls, equipment, key);

			IcdStopwatch stopwatch = IcdStopwatch.StartNew();
			for (uint index = 0; index < 100; index++)
				ping.AddSig(new Sig(index, 0, "ping"));
			IcdConsole.PrintLine("{0} milliseconds to create sigs", stopwatch.ElapsedMilliseconds);

			SendInputData(ping);
		}

		/// <summary>
		/// Sends a pong to the remote crosspoint/s.
		/// </summary>
		private void Pong(CrosspointData ping)
		{
			string key = JsonConvert.DeserializeObject<string>(ping.GetJson().First());
			int[] controls = ping.GetControlIds().ToArray();
			int equipment = ping.EquipmentId;

			IcdConsole.PrintLine("{0} {1} sending Pong - Controls={2}, Equipment={3}", GetType().Name, Id,
			                          StringUtils.ArrayFormat(controls), equipment);

			CrosspointData pong = CrosspointData.Pong(controls, equipment, key);

			IcdStopwatch stopwatch = IcdStopwatch.StartNew();
			for (uint index = 0; index < 100; index++)
				pong.AddSig(new Sig(index, 0, "pong"));
			IcdConsole.PrintLine("{0} milliseconds to create sigs", stopwatch.ElapsedMilliseconds);

			SendInputData(pong);
		}

		/// <summary>
		/// Handle receiving a pong message.
		/// </summary>
		/// <param name="pong"></param>
		private void ReceivePong(CrosspointData pong)
		{
			IcdConsole.PrintLine("{0} {1} received Pong - Controls={2}, Equipment={3}", GetType().Name, Id,
			                          StringUtils.ArrayFormat(pong.GetControlIds()), pong.EquipmentId);

			string key = JsonConvert.DeserializeObject<string>(pong.GetJson().First());
			DateTime pingTime;

			m_PingTimesSection.Enter();

			try
			{
				if (!m_PingTimes.TryGetValue(key, out pingTime))
					return;

				m_PingTimes.Remove(key);
			}
			finally
			{
				m_PingTimesSection.Leave();
			}

			IcdConsole.PrintLine("Time - {0}ms", (IcdEnvironment.GetLocalTime() - pingTime).TotalMilliseconds);
		}

		/// <summary>
		/// Handle receiving a ping message.
		/// </summary>
		/// <param name="ping"></param>
		private void ReceivePing(CrosspointData ping)
		{
			IcdConsole.PrintLine("{0} {1} received Ping - Controls={2}, Equipment={3}", GetType().Name, Id,
			                          StringUtils.ArrayFormat(ping.GetControlIds()), ping.EquipmentId);

			Pong(ping);
		}

		/// <summary>
		/// Simple method to make sure crosspoints don't eat a bunch of memory.
		/// </summary>
		private void ClearOldPingTimes()
		{
			m_PingTimesSection.Enter();

			try
			{
				DateTime now = IcdEnvironment.GetLocalTime();
				m_PingTimes.RemoveAll(kvp => (now - kvp.Value).TotalSeconds > PING_TIMEOUT_SECONDS);
			}
			finally
			{
				m_PingTimesSection.Leave();
			}
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public virtual IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield break;
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public virtual void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("Id", m_Id);
			addRow("Name", m_Name);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public virtual IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new ConsoleCommand("Ping", "Sends a ping to the connected crosspoint/s", () => Ping());
		}

		#endregion
	}

	public enum eCrosspointStatus
	{
		Uninitialized = 0,
		Idle = 1,
		Connected = 2,
		EquipmentNotFound = 3,
		ConnectFailed = 4,
		ConnectionDropped = 5,
		ConnectionClosedRemote = 6,
	}
}
