using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.Json;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.Network.Attributes.Rpc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#if SIMPLSHARP
using Crestron.SimplSharp.Reflection;
#else
using System.Reflection;
#endif

namespace ICD.Connect.Protocol.Network.RemoteProcedure
{
	/// <summary>
	/// Remote Procedure Call. Contains the destination member and associated parameters.
	/// </summary>
	public sealed class Rpc : ISerialData
	{
		public enum eProcedureType
		{
			Method = 0,
			PropertySetter = 1
		}

		// Keeping the tokens nice and small for less TCP data.
		private const string PROCEDURE_TYPE_TOKEN = "t";
		private const string KEY_TOKEN = "k";
		private const string PARAMETERS_TOKEN = "p";

		private readonly eProcedureType m_ProcedureType;
		private readonly string m_Key;
		private readonly List<object> m_Parameters;

		/// <summary>
		/// Gets the RPC destination type.
		/// </summary>
		public eProcedureType ProcedureType { get { return m_ProcedureType; } }

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="procedureType"></param>
		/// <param name="key"></param>
		/// <param name="parameters"></param>
		private Rpc(eProcedureType procedureType, string key, IEnumerable<object> parameters)
		{
			m_ProcedureType = procedureType;
			m_Key = key;
			m_Parameters = new List<object>(parameters);
		}

		/// <summary>
		/// Instantiates an RPC that sets the property with the given key.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		[PublicAPI]
		public static Rpc SetPropertyRpc(string key, object value)
		{
			return new Rpc(eProcedureType.PropertySetter, key, new[] {value});
		}

		/// <summary>
		/// Instantiates an RPC that calls the method with the given name.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="values"></param>
		/// <returns></returns>
		[PublicAPI]
		public static Rpc CallMethodRpc(string key, params object[] values)
		{
			return new Rpc(eProcedureType.Method, key, values);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Executes the RPC on the given instance.
		/// </summary>
		/// <param name="client"></param>
		public void Execute(object client)
		{
			switch (m_ProcedureType)
			{
				case eProcedureType.Method:
					ExecuteMethod(client);
					return;
				case eProcedureType.PropertySetter:
					ExecutePropertySetter(client);
					return;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Inserts the client id at the start of the collection of parameters.
		/// </summary>
		/// <param name="clientId"></param>
		/// <exception cref="NotSupportedException">RPC type is not a method.</exception>
		public void PrependClientId(uint clientId)
		{
			if (m_ProcedureType != eProcedureType.Method)
				throw new NotSupportedException("Can not prepend client id unless the target is a method.");
			m_Parameters.Insert(0, clientId);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Executes the RPC as a method call on the given client.
		/// </summary>
		/// <param name="client"></param>
		private void ExecuteMethod(object client)
		{
			MethodInfo method = RpcAttribute.GetMethod(client, m_Key, m_Parameters);

			if (method == null)
			{
				string message = string.Format("RPC unable to find Method with key \"{0}\" for type {1}", m_Key,
				                               client.GetType().Name);
				throw new KeyNotFoundException(message);
			}

			try
			{
				method.Invoke(client, m_Parameters.ToArray());
			}
			catch (Exception e)
			{
				// Get the real exception, not the TargetInvocationException.
				throw e.InnerException;
			}
		}

		/// <summary>
		/// Executes the RPC as a property setter on the given client.
		/// </summary>
		/// <param name="client"></param>
		private void ExecutePropertySetter(object client)
		{
			PropertyInfo property = RpcAttribute.GetProperty(client, m_Key, m_Parameters.First());

			if (property == null)
			{
				string message = string.Format("RPC unable to find Property with key \"{0}\" for type {1}", m_Key,
				                               client.GetType().Name);
				throw new KeyNotFoundException(message);
			}

			try
			{
				property.SetValue(client, m_Parameters[0], null);
			}
			catch (Exception e)
			{
				// Get the real exception, not the TargetInvocationException.
				throw e.InnerException;
			}
		}

		#endregion

		#region Serialization

		/// <summary>
		/// Instantiates the RPC from a JSON string.
		/// </summary>
		/// <returns></returns>
		public static Rpc Deserialize(string data)
		{
			JObject json = JObject.Parse(data);

			int procedureType = (int)json.SelectToken(PROCEDURE_TYPE_TOKEN);
			string key = (string)json.SelectToken(KEY_TOKEN);
			JToken paramsArray = json.SelectToken(PARAMETERS_TOKEN);

			object[] parameters = paramsArray.AsJEnumerable()
			                                 .Select(t => JsonItemWrapper.ReadToObject(t))
			                                 .ToArray();

			return new Rpc((eProcedureType)procedureType, key, parameters);
		}

		/// <summary>
		/// Returns the RPC as a JSON string representation.
		/// </summary>
		/// <returns></returns>
		public string Serialize()
		{
			return JsonUtils.Serialize(Serialize);
		}

		public void Serialize(JsonWriter writer)
		{
			writer.WriteStartObject();
			{
				WriteProcedureType(writer);
				WriteKey(writer);
				WriteParameters(writer);
			}
			writer.WriteEndObject();
		}

		/// <summary>
		/// Writes the procedure type to JSON.
		/// </summary>
		/// <param name="writer"></param>
		private void WriteProcedureType(JsonWriter writer)
		{
			writer.WritePropertyName(PROCEDURE_TYPE_TOKEN);
			writer.WriteValue((int)m_ProcedureType);
		}

		/// <summary>
		/// Writes the key to JSON.
		/// </summary>
		/// <param name="writer"></param>
		private void WriteKey(JsonWriter writer)
		{
			writer.WritePropertyName(KEY_TOKEN);
			writer.WriteValue(m_Key);
		}

		/// <summary>
		/// Writes the parameters to JSON.
		/// </summary>
		/// <param name="writer"></param>
		private void WriteParameters(JsonWriter writer)
		{
			writer.WritePropertyName(PARAMETERS_TOKEN);
			writer.WriteStartArray();

			foreach (JsonItemWrapper wrapper in m_Parameters.Select(p => new JsonItemWrapper(p)))
				wrapper.Write(writer);

			writer.WriteEndArray();
		}

		#endregion
	}
}
