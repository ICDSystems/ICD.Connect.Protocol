using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
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
		public string ConsoleName { get { return Name ?? GetType().GetNameWithoutGenericArity(); } }

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

				Logger.AddEntry(eSeverity.Informational, "{0} status changed to {1}", this, m_Status);

				OnStatusChanged.Raise(this, new CrosspointStatusEventArgs(m_Status));
			}
		}

		/// <summary>
		/// When enabled prints the sent sigs to the console.
		/// </summary>
		[PublicAPI]
		public bool DebugInput { get; set; }

		/// <summary>
		/// When enabled prints the received sigs to the console.
		/// </summary>
		[PublicAPI]
		public bool DebugOutput { get; set; }

		/// <summary>
		/// Gets the logger for this crosspoint.
		/// </summary>
		protected ILoggerService Logger { get { return ServiceProvider.TryGetService<ILoggerService>(); } }

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

			m_Status = eCrosspointStatus.Idle;
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public virtual void Dispose()
		{
			OnSendOutputData = null;
			OnSendInputData = null;
			OnStatusChanged = null;
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

			PrintOutput(data);

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
			if (data.ControlIdsCount == 0)
				data.AddControlIds(GetControlsForMessage());
			if (data.EquipmentId == 0)
				data.EquipmentId = GetEquipmentForMessage();

			PrintInput(data);

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
			ReprBuilder builder = new ReprBuilder(this);

			builder.AppendProperty("Id", Id);
			builder.AppendProperty("Name", Name);

			return builder.ToString();
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// When input debugging is enabled, prints the data to the console.
		/// </summary>
		/// <param name="data"></param>
		private void PrintInput(CrosspointData data)
		{
			if (DebugInput)
				PrintData("Input", data);
		}

		/// <summary>
		/// When output debugging is enabled, prints the data to the console.
		/// </summary>
		/// <param name="data"></param>
		private void PrintOutput(CrosspointData data)
		{
			if (DebugOutput)
				PrintData("Output", data);
		}

		/// <summary>
		/// Prints the given data to the console.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="data"></param>
		private void PrintData(string context, CrosspointData data)
		{
			IcdConsole.PrintLine("{0} {1} - {2}", this, context, data);
			foreach (SigInfo sig in data.GetSigs())
				IcdConsole.PrintLine("{0} {1} - {2}", this, context, sig);
		}

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
				ping.AddSig(new SigInfo(index, 0, "ping"));
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
				pong.AddSig(new SigInfo(index, 0, "pong"));
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

				string[] remove = m_PingTimes.Where(kvp => (now - kvp.Value).TotalSeconds > PING_TIMEOUT_SECONDS)
				                             .Select(kvp => kvp.Key)
				                             .ToArray();

				foreach (string key in remove)
					m_PingTimes.Remove(key);
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
			addRow("Status", m_Status);
			addRow("Debug Input", DebugInput);
			addRow("Debug Output", DebugOutput);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public virtual IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new ConsoleCommand("EnableDebug",
			                                "Starts printing both input and output sigs to console",
			                                () =>
			                                {
				                                DebugOutput = true;
				                                DebugInput = true;
			                                });

			yield return new ConsoleCommand("DisableDebug",
			                                "Stops printing both input and output sigs to console",
			                                () =>
			                                {
				                                DebugOutput = false;
				                                DebugInput = false;
			                                });

			yield return new ConsoleCommand("ToggleDebugInput", "When enabled prints input sigs to console",
			                                () => DebugInput = !DebugInput);
			yield return new ConsoleCommand("ToggleDebugOutput", "When enabled prints output sigs to console",
			                                () => DebugOutput = !DebugOutput);

			yield return new ConsoleCommand("Ping", "Sends a ping to the connected crosspoint/s", () => Ping());
			yield return new ConsoleCommand("PrintSigs", "Prints the cached sigs", () => PrintSigs());

			yield return
				new GenericConsoleCommand<ushort, uint, ushort>("SendInputAnalog",
				                                                "SendInputAnalog <SMARTOBJECT> <NUMBER> <0-65535>",
				                                                (s, n, a) => SendInputSig(s, n, a));

			yield return
				new GenericConsoleCommand<ushort, uint, bool>("SendInputDigital",
				                                              "SendInputDigital <SMARTOBJECT> <NUMBER> <true/false>",
				                                              (s, n, b) => SendInputSig(s, n, b));

			yield return
				new GenericConsoleCommand<ushort, uint, string>("SendInputSerial",
				                                                "SendInputSerial <SMARTOBJECT> <NUMBER> <SERIAL>",
				                                                (s, n, v) => SendInputSig(s, n, v));
		}

		protected abstract string PrintSigs();

		protected string PrintSigs(SigCache cache)
		{
			if (cache == null)
				throw new ArgumentNullException("cache");

			TableBuilder builder = new TableBuilder("Type", "Smart Object", "Number", "Name", "Value");

			IEnumerable<SigInfo> sigs =
				cache.OrderBy(s => s.SmartObject)
				     .ThenBy(s => s.Type)
				     .ThenBy(s => s.Number)
				     .ThenBy(s => s.Name);

			foreach (SigInfo sig in sigs)
				builder.AddRow(sig.Type, sig.SmartObject, sig.Number, StringUtils.ToRepresentation(sig.Name), sig.GetValue());

			return builder.ToString();
		}

		#endregion
	}

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
