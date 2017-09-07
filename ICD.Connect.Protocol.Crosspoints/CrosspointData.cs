using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.IO;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.Sigs;
using Newtonsoft.Json;

namespace ICD.Connect.Protocol.Crosspoints
{
	/// <summary>
	/// Wrapper for all messages passing between Equipment and Control crosspoints.
	/// </summary>
	public sealed class CrosspointData : ISerialData
	{
		public const char MESSAGE_TERMINATOR = (char)0xFF;

		// Json
		private const string MESSAGE_TYPE_PROPERTY = "T";
		private const string EQUIPMENT_ID_PROPERTY = "EId";
		private const string CONTROL_IDS_PROPERTY = "CIds";
		private const string JSON_PROPERTY = "J";
		private const string SIGS_PROPERTY = "S";

		public enum eMessageType
		{
			// Generic message from one crosspoint to another.
			Message = 0,

			// Control messages
			ControlConnect = 1000,
			ControlDisconnect = 1001,
			ControlClear = 1002,

			// Equipment messages
			EquipmentConnect = 2000,
			EquipmentDisconnect = 2001,

			// Debug Messages
			Ping = 3000,
			Pong = 3001
		}

		private readonly IcdHashSet<int> m_ControlIds;
		private readonly IcdHashSet<string> m_Json;
		private readonly SigCache m_Sigs;

		#region Properties

		/// <summary>
		/// Gets/sets the equipment this data is addressed from/to.
		/// </summary>
		[PublicAPI]
		public int EquipmentId { get; set; }

		[PublicAPI]
		public eMessageType MessageType { get; private set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public CrosspointData()
		{
			m_ControlIds = new IcdHashSet<int>();
			m_Json = new IcdHashSet<string>();
			m_Sigs = new SigCache();
		}

		/// <summary>
		/// Creates a Connect message, used by control crosspoints to inform an equipment crosspoint that
		/// they are connected.
		/// </summary>
		/// <param name="controlId"></param>
		/// <param name="equipmentId"></param>
		/// <returns></returns>
		[PublicAPI]
		public static CrosspointData ControlConnect(int controlId, int equipmentId)
		{
			return CreateMessage(controlId, equipmentId, eMessageType.ControlConnect);
		}

		/// <summary>
		/// Creates a Disconnect message, used by control crosspoints to inform an equipment crosspoint that
		/// they are disconnected.
		/// </summary>
		/// <param name="controlId"></param>
		/// <param name="equipmentId"></param>
		/// <returns></returns>
		[PublicAPI]
		public static CrosspointData ControlDisconnect(int controlId, int equipmentId)
		{
			return CreateMessage(controlId, equipmentId, eMessageType.ControlDisconnect);
		}

		/// <summary>
		/// Creates a control clear message. Used by crosspoints to clear themselves.
		/// </summary>
		/// <param name="controlId"></param>
		/// <param name="equipmentId"></param>
		/// <param name="sigs"></param>
		/// <returns></returns>
		[PublicAPI]
		public static CrosspointData ControlClear(int controlId, int equipmentId, IEnumerable<SigInfo> sigs)
		{
			CrosspointData output = CreateMessage(controlId, equipmentId, eMessageType.ControlClear);

			output.AddSigs(sigs);

			return output;
		}

		/// <summary>
		/// Creates an equipment connect message. Sends the current status of the equipment to the control.
		/// </summary>
		/// <param name="controlId"></param>
		/// <param name="equipmentId"></param>
		/// <param name="sigs"></param>
		/// <returns></returns>
		[PublicAPI]
		public static CrosspointData EquipmentConnect(int controlId, int equipmentId, IEnumerable<SigInfo> sigs)
		{
			CrosspointData output = CreateMessage(controlId, equipmentId, eMessageType.EquipmentConnect);

			IEnumerable<SigInfo> initialize = sigs.Where(s => s.HasValue());
			output.AddSigs(initialize);

			return output;
		}

		/// <summary>
		/// Creates an equipment disconnect message.
		/// </summary>
		/// <param name="controlId"></param>
		/// <param name="equipmentId"></param>
		/// <returns></returns>
		[PublicAPI]
		public static CrosspointData EquipmentDisconnect(int controlId, int equipmentId)
		{
			return CreateMessage(controlId, equipmentId, eMessageType.EquipmentDisconnect);
		}

		/// <summary>
		/// Creates a ping message.
		/// </summary>
		/// <param name="controlIds"></param>
		/// <param name="equipmentId"></param>
		/// <param name="key"></param>
		/// <returns></returns>
		public static CrosspointData Ping(IEnumerable<int> controlIds, int equipmentId, string key)
		{
			CrosspointData output = CreateMessage(controlIds, equipmentId, eMessageType.Ping);
			output.AddJson(JsonConvert.SerializeObject(key));

			return output;
		}

		/// <summary>
		/// Creates a pong message.
		/// </summary>
		/// <param name="controlIds"></param>
		/// <param name="equipmentId"></param>
		/// <param name="key"></param>
		/// <returns></returns>
		public static CrosspointData Pong(IEnumerable<int> controlIds, int equipmentId, string key)
		{
			CrosspointData output = CreateMessage(controlIds, equipmentId, eMessageType.Pong);
			output.AddJson(JsonConvert.SerializeObject(key));

			return output;
		}

		/// <summary>
		/// Creates a message of the given type.
		/// </summary>
		/// <param name="controlId"></param>
		/// <param name="equipmentId"></param>
		/// <param name="messageType"></param>
		/// <returns></returns>
		private static CrosspointData CreateMessage(int controlId, int equipmentId, eMessageType messageType)
		{
			return CreateMessage(new[] {controlId}, equipmentId, messageType);
		}

		/// <summary>
		/// Creates a message of the given type.
		/// </summary>
		/// <param name="controlIds"></param>
		/// <param name="equipmentId"></param>
		/// <param name="messageType"></param>
		/// <returns></returns>
		private static CrosspointData CreateMessage(IEnumerable<int> controlIds, int equipmentId, eMessageType messageType)
		{
			CrosspointData output = new CrosspointData
			{
				EquipmentId = equipmentId,
				MessageType = messageType
			};
			output.AddControlIds(controlIds);
			return output;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Gets the controls this data is addressed from/to.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public IEnumerable<int> GetControlIds()
		{
			return m_ControlIds.ToArray();
		}

		/// <summary>
		/// Adds the given control to the message.
		/// </summary>
		/// <param name="controlId"></param>
		[PublicAPI]
		public void AddControlId(int controlId)
		{
			m_ControlIds.Add(controlId);
		}

		/// <summary>
		/// Adds the controls to the message.
		/// </summary>
		/// <param name="controls"></param>
		[PublicAPI]
		public void AddControlIds(IEnumerable<int> controls)
		{
			foreach (int item in controls)
				AddControlId(item);
		}

		/// <summary>
		/// Adds JSON to the data.
		/// </summary>
		/// <param name="json"></param>
		public void AddJson(string json)
		{
			m_Json.Add(json);
		}

		/// <summary>
		/// Adds the JSON strings to the message.
		/// </summary>
		/// <param name="json"></param>
		public void AddJson(IEnumerable<string> json)
		{
			foreach (string item in json)
				AddJson(item);
		}

		/// <summary>
		/// Removes JSON from the data.
		/// </summary>
		/// <param name="json"></param>
		public void RemoveJson(string json)
		{
			m_Json.Remove(json);
		}

		/// <summary>
		/// Gets all of the JSON in the data.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<string> GetJson()
		{
			return m_Json.ToArray();
		}

		/// <summary>
		/// Adds the sig to the data.
		/// </summary>
		/// <param name="sig"></param>
		public void AddSig(SigInfo sig)
		{
			m_Sigs.Add(sig);
		}

		/// <summary>
		/// Adds the sig to the data.
		/// </summary>
		/// <param name="smartObject"></param>
		/// <param name="number"></param>
		/// <param name="value"></param>
		public void AddSig(ushort smartObject, uint number, bool value)
		{
			SigInfo sig = new SigInfo(number, smartObject, value);
			AddSig(sig);
		}

		/// <summary>
		/// Adds the sig to the data.
		/// </summary>
		/// <param name="smartObject"></param>
		/// <param name="number"></param>
		/// <param name="value"></param>
		public void AddSig(ushort smartObject, uint number, ushort value)
		{
			SigInfo sig = new SigInfo(number, smartObject, value);
			AddSig(sig);
		}

		/// <summary>
		/// Adds the sig to the data.
		/// </summary>
		/// <param name="smartObject"></param>
		/// <param name="number"></param>
		/// <param name="value"></param>
		public void AddSig(ushort smartObject, uint number, string value)
		{
			SigInfo sig = new SigInfo(number, smartObject, value);
			AddSig(sig);
		}

		/// <summary>
		/// Removes the sig from the data.
		/// </summary>
		/// <param name="sig"></param>
		public void RemoveSig(SigInfo sig)
		{
			m_Sigs.Remove(sig);
		}

		/// <summary>
		/// Adds the sigs to the data.
		/// </summary>
		/// <param name="sigs"></param>
		public void AddSigs(IEnumerable<SigInfo> sigs)
		{
			m_Sigs.AddRange(sigs);
		}

		/// <summary>
		/// Gets the sigs in the data.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<SigInfo> GetSigs()
		{
			return m_Sigs.ToArray();
		}

		/// <summary>
		/// Gets a string representation for the crosspoint data.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return string.Format("{0}(MessageType={1}, EquipmentId={2}, ControlIds={3}, SigsCount={4})",
			                     GetType().Name, MessageType, EquipmentId, StringUtils.ArrayFormat(GetControlIds()), m_Sigs.Count);
		}

		#endregion

		#region Serialization

		/// <summary>
		/// Serializes the CrosspointData to a JSON string.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public string Serialize()
		{
			StringBuilder builder = new StringBuilder();

			using (JsonTextWriter writer = new JsonTextWriter(new IcdStringWriter(builder).WrappedStringWriter))
				Serialize(writer);

			return builder.ToString() + MESSAGE_TERMINATOR;
		}

		/// <summary>
		/// Serializes the CrosspointData to JSON.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public void Serialize(JsonWriter writer)
		{
			//Stopwatch stopwatch = Stopwatch.StartNew();

			try
			{
				writer.WriteStartObject();
				{
					writer.WritePropertyName(MESSAGE_TYPE_PROPERTY);
					writer.WriteValue(MessageType.ToString());

					if (EquipmentId != 0)
					{
						writer.WritePropertyName(EQUIPMENT_ID_PROPERTY);
						writer.WriteValue(EquipmentId);
					}

					if (m_ControlIds.Count > 0)
					{
						writer.WritePropertyName(CONTROL_IDS_PROPERTY);
						writer.WriteStartArray();
						{
							foreach (int id in m_ControlIds)
								writer.WriteValue(id);
						}
						writer.WriteEndArray();
					}

					if (m_Json.Count > 0)
					{
						writer.WritePropertyName(JSON_PROPERTY);
						writer.WriteStartArray();
						{
							foreach (string item in m_Json)
								writer.WriteValue(item);
						}
						writer.WriteEndArray();
					}

					if (m_Sigs.Count > 0)
					{
						writer.WritePropertyName(SIGS_PROPERTY);
						writer.WriteStartArray();
						{
							foreach (SigInfo sig in m_Sigs)
								sig.Serialize(writer);
						}
						writer.WriteEndArray();
					}
				}
				writer.WriteEndObject();
			}
			finally
			{
			//	CrestronConsole.PrintLine("{0} milliseconds to serialize {1}(MessageType={2})",
			//	                          stopwatch.Elapsed.TotalMilliseconds, GetType().Name, MessageType);
			}
		}

		/// <summary>
		/// Deserializes the CrosspointData from a JSON string.
		/// </summary>
		/// <param name="json"></param>
		/// <returns></returns>
		[PublicAPI]
		public static CrosspointData Deserialize(string json)
		{
			using (JsonTextReader reader = new JsonTextReader(new IcdStringReader(json).WrappedStringReader))
				return Deserialize(reader);
		}

		/// <summary>
		/// Deserializes the CrosspointData from a JSON reader.
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		[PublicAPI]
		public static CrosspointData Deserialize(JsonReader reader)
		{
			//Stopwatch stopwatch = Stopwatch.StartNew();

			CrosspointData output = null;

			try
			{
				eMessageType type = eMessageType.Message;
				int equipmentId = 0;
				IcdHashSet<int> controlIds = new IcdHashSet<int>();
				IcdHashSet<string> json = new IcdHashSet<string>();
				SigCache sigs = new SigCache();

				while (reader.Read())
				{
					if (reader.TokenType == JsonToken.EndObject)
					{
						reader.Read();
						break;
					}

					if (reader.TokenType != JsonToken.PropertyName)
						continue;

					string property = reader.Value as string;

					// Read to the value
					reader.Read();

					switch (property)
					{
						case MESSAGE_TYPE_PROPERTY:
							type = reader.GetValueAsEnum<eMessageType>();
							break;

						case EQUIPMENT_ID_PROPERTY:
							equipmentId = reader.GetValueAsInt();
							break;

						case CONTROL_IDS_PROPERTY:
							while (reader.Read() && reader.TokenType != JsonToken.EndArray)
								controlIds.Add(reader.GetValueAsInt());
							break;

						case JSON_PROPERTY:
							while (reader.Read() && reader.TokenType != JsonToken.EndArray)
								json.Add(reader.GetValueAsString());
							break;

						case SIGS_PROPERTY:
							while (reader.TokenType != JsonToken.EndArray)
								sigs.Add(SigInfo.Deserialize(reader));
							break;
					}
				}

				output = new CrosspointData
				{
					MessageType = type,
					EquipmentId = equipmentId,
				};

				output.AddControlIds(controlIds);
				output.AddJson(json);
				output.AddSigs(sigs);

				return output;
			}
			finally
			{
			//	CrestronConsole.PrintLine("{0} milliseconds to deserialize to {1}(MessageType={2})",
			//	                          stopwatch.Elapsed.TotalMilliseconds, typeof(CrosspointData).Name, output.MessageType);
			}
		}

		#endregion
	}
}
