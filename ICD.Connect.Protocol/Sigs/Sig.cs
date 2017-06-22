using System;
using System.Text;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.IO;
using Newtonsoft.Json;

namespace ICD.Connect.Protocol.Sigs
{
	public struct Sig : IUShortOutputSig, IStringOutputSig, IBoolOutputSig
	{
		// JSON
		private const string TYPE_PROPERTY = "T";
		private const string NUMBER_PROPERTY = "No";
		private const string NAME_PROPERTY = "Na";
		private const string SMARTOBJECT_PROPERTY = "SO";
		private const string VALUE_PROPERTY = "V";

		private readonly eSigType m_Type;
		private readonly uint m_Number;
		private readonly string m_Name;
		private readonly ushort m_SmartObject;

		private readonly bool m_BoolValue;
		private readonly ushort m_UshortValue;
		private readonly string m_StringValue;

		#region Properties

		/// <summary>
		/// Type of data this sig uses when communicating with the device.
		/// </summary>
		public eSigType Type { get { return m_Type; } }

		/// <summary>
		/// Number of this sig.
		/// </summary>
		public uint Number { get { return m_Number; } }

		/// <summary>
		/// Get/Set the name of this Sig.
		/// </summary>
		public string Name { get { return m_Name; } }

		/// <summary>
		/// Gets the SmartObject this sig is associated with.
		/// </summary>
		public ushort SmartObject { get { return m_SmartObject; } }

		#endregion

		#region Constructors

		public Sig(uint number, ushort smartObject, string value)
			: this(number, null, smartObject, value)
		{
		}

		public Sig(uint number, ushort smartObject, bool value)
			: this(number, null, smartObject, value)
		{
		}

		public Sig(uint number, ushort smartObject, ushort value)
			: this(number, null, smartObject, value)
		{
		}

		public Sig(string name, ushort smartObject, string value)
			: this(0, name, smartObject, value)
		{
		}

		public Sig(string name, ushort smartObject, bool value)
			: this(0, name, smartObject, value)
		{
		}

		public Sig(string name, ushort smartObject, ushort value)
			: this(0, name, smartObject, value)
		{
		}

		public Sig(uint number, string name, ushort smartObject, string value)
			: this(eSigType.Serial, number, name, smartObject)
		{
			m_StringValue = value;
		}

		public Sig(uint number, string name, ushort smartObject, bool value)
			: this(eSigType.Digital, number, name, smartObject)
		{
			m_BoolValue = value;
		}

		public Sig(uint number, string name, ushort smartObject, ushort value)
			: this(eSigType.Analog, number, name, smartObject)
		{
			m_UshortValue = value;
		}

		private Sig(eSigType type, uint number, string name, ushort smartObject)
		{
			m_Type = type;
			m_Number = number;
			m_Name = name;
			m_SmartObject = smartObject;

			m_StringValue = null;
			m_BoolValue = false;
			m_UshortValue = 0;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Gets a copy of the sig with empty values.
		/// </summary>
		/// <returns></returns>
		public Sig ToClearSig()
		{
			return new Sig(m_Type, m_Number, m_Name, m_SmartObject);
		}

		/// <summary>
		/// Get the string representation of this Sig.
		/// </summary>
		/// <exception cref="T:System.InvalidOperationException">Sig is in an invalid state.</exception>
		public string GetStringValue()
		{
			if (m_Type != eSigType.Serial)
				throw new InvalidOperationException();
			return m_StringValue;
		}

		/// <summary>
		/// Get the UShort representation of this Sig.
		/// </summary>
		/// <exception cref="T:System.InvalidOperationException">Sig is in an invalid state.</exception>
		public ushort GetUShortValue()
		{
			if (m_Type != eSigType.Analog)
				throw new InvalidOperationException();
			return m_UshortValue;
		}

		/// <summary>
		/// Get the bool representation of this Sig.
		/// </summary>
		/// <exception cref="T:System.InvalidOperationException">Sig is in an invalid state.</exception>
		public bool GetBoolValue()
		{
			if (m_Type != eSigType.Digital)
				throw new InvalidOperationException();
			return m_BoolValue;
		}

		/// <summary>
		/// Gets the string representation for this value.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return string.Format("{0}(Type={1}, Number={2}, Name={3}, SmartObject={4}, Value={5})",
			                     GetType().Name, m_Type, m_Number, m_Name, m_SmartObject, this.GetValue());
		}

		#endregion

		#region Equality

		public override bool Equals(object obj)
		{
			return obj is Sig && this == (Sig)obj;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;

				hash = hash * 23 + (int)m_Type;
				hash = hash * 23 + (int)m_Number;
				hash = hash * 23 + (m_Name == null ? 0 : m_Name.GetHashCode());
				hash = hash * 23 + m_SmartObject;

				hash = hash * 23 + m_UshortValue;
				hash = hash * 23 + (m_BoolValue ? 1 : 0);
				hash = hash * 23 + (m_StringValue == null ? 0 : m_StringValue.GetHashCode());

				return hash;
			}
		}

		public static bool operator ==(Sig x, Sig y)
		{
			bool output = x.m_Type == y.m_Type &&
						  x.m_Number == y.m_Number &&
						  x.m_Name == y.m_Name &&
						  x.m_SmartObject == y.m_SmartObject &&
						  x.m_BoolValue == y.m_BoolValue &&
						  x.m_UshortValue == y.m_UshortValue &&
						  x.m_StringValue == y.m_StringValue;

			return output;
		}

		public static bool operator !=(Sig x, Sig y)
		{
			return !(x == y);
		}

		#endregion

		#region Serialization

		/// <summary>
		/// Serializes the sig to JSON.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public string Serialize()
		{
			StringBuilder builder = new StringBuilder();

			using (JsonTextWriter writer = new JsonTextWriter(new IcdStringWriter(builder).WrappedStringWriter))
				Serialize(writer);

			return builder.ToString();
		}

		/// <summary>
		/// Serializes the sig to JSON.
		/// </summary>
		/// <param name="writer"></param>
		[PublicAPI]
		public void Serialize(JsonWriter writer)
		{
			writer.WriteStartObject();
			{
				writer.WritePropertyName(TYPE_PROPERTY);
				writer.WriteValue(m_Type.ToString());

				if (m_Number != 0)
				{
					writer.WritePropertyName(NUMBER_PROPERTY);
					writer.WriteValue(m_Number);
				}

				if (!string.IsNullOrEmpty(m_Name))
				{
					writer.WritePropertyName(NAME_PROPERTY);
					writer.WriteValue(m_Name);
				}

				if (m_SmartObject != 0)
				{
					writer.WritePropertyName(SMARTOBJECT_PROPERTY);
					writer.WriteValue(m_SmartObject);
				}

				if (this.HasValue())
				{
					writer.WritePropertyName(VALUE_PROPERTY);
					switch (m_Type)
					{
						case eSigType.Digital:
							writer.WriteValue(m_BoolValue);
							break;
						case eSigType.Analog:
							writer.WriteValue(m_UshortValue);
							break;
						case eSigType.Serial:
							writer.WriteValue(m_StringValue);
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}
			}
			writer.WriteEndObject();
		}

		/// <summary>
		/// Deserializes the sig from json.
		/// </summary>
		/// <param name="json"></param>
		/// <returns></returns>
		[PublicAPI]
		public static Sig Deserialize(string json)
		{
			using (JsonTextReader reader = new JsonTextReader(new IcdStringReader(json).WrappedStringReader))
				return Deserialize(reader);
		}

		/// <summary>
		/// Deserializes the sig from json.
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		[PublicAPI]
		public static Sig Deserialize(JsonReader reader)
		{
			eSigType type = eSigType.Na;
			uint number = 0;
			string name = null;
			ushort smartObject = 0;

			bool boolValue = false;
			ushort ushortValue = 0;
			string stringValue = null;

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
					case TYPE_PROPERTY:
						type = reader.GetValueAsEnum<eSigType>();
						break;

					case NUMBER_PROPERTY:
						number = (uint)reader.GetValueAsInt();
						break;

					case NAME_PROPERTY:
						name = reader.GetValueAsString();
						break;

					case SMARTOBJECT_PROPERTY:
						smartObject = (ushort)reader.GetValueAsInt();
						break;

					case VALUE_PROPERTY:
						switch (reader.TokenType)
						{
							case JsonToken.Boolean:
								boolValue = reader.GetValueAsBool();
								break;

							case JsonToken.Integer:
								ushortValue = (ushort)reader.GetValueAsInt();
								break;

							case JsonToken.String:
							case JsonToken.Null:
								stringValue = reader.GetValueAsString();
								break;

							default:
								throw new ArgumentOutOfRangeException();
						}
						break;
				}
			}

			switch (type)
			{
				case eSigType.Digital:
					return new Sig(number, name, smartObject, boolValue);
				case eSigType.Analog:
					return new Sig(number, name, smartObject, ushortValue);
				case eSigType.Serial:
					return new Sig(number, name, smartObject, stringValue);

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		#endregion
	}
}
