#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.Network.Attributes.Rpc;
#if SIMPLSHARP
using Crestron.SimplSharp.Reflection;
#else
using System.Reflection;
using System.Runtime.ExceptionServices;
#endif

namespace ICD.Connect.Protocol.Network.RemoteProcedure
{
	/// <summary>
	/// Remote Procedure Call. Contains the destination member and associated parameters.
	/// </summary>
	[JsonConverter(typeof(RpcConverter))]
	public sealed class Rpc : ISerialData
	{
		public enum eProcedureType
		{
			Method = 0,
			PropertySetter = 1
		}

		private readonly List<object> m_Parameters;

		#region Properties

		/// <summary>
		/// Gets the RPC destination type.
		/// </summary>
		public eProcedureType ProcedureType { get; set; }

		/// <summary>
		/// Gets the RPC key name.
		/// </summary>
		public string Key { get; set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public Rpc()
		{
			m_Parameters = new List<object>();
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
			Rpc output = new Rpc
			{
				ProcedureType = eProcedureType.PropertySetter,
				Key = key
			};
			output.SetParameters(value.Yield());

			return output;
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
			Rpc output = new Rpc
			{
				ProcedureType = eProcedureType.Method,
				Key = key
			};
			output.SetParameters(values);

			return output;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Gets the parameter values.
		/// </summary>
		/// <returns></returns>
		[NotNull]
		public IEnumerable<object> GetParameters()
		{
			return m_Parameters.ToArray();
		}

		/// <summary>
		/// Sets the parameter values.
		/// </summary>
		/// <param name="parameters"></param>
		public void SetParameters([NotNull] IEnumerable<object> parameters)
		{
			m_Parameters.Clear();
			m_Parameters.AddRange(parameters);
		}

		/// <summary>
		/// Executes the RPC on the given instance.
		/// </summary>
		/// <param name="client"></param>
		public void Execute(object client)
		{
			switch (ProcedureType)
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
			if (ProcedureType != eProcedureType.Method)
				throw new NotSupportedException("Can not prepend client id unless the target is a method.");
			m_Parameters.Insert(0, clientId);
		}

		/// <summary>
		/// Returns the RPC as a JSON string representation.
		/// </summary>
		/// <returns></returns>
		public string Serialize()
		{
			return JsonConvert.SerializeObject(this);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Executes the RPC as a method call on the given client.
		/// </summary>
		/// <param name="client"></param>
		private void ExecuteMethod(object client)
		{
			MethodInfo method = RpcAttribute.GetMethod(client, Key, m_Parameters);

			if (method == null)
			{
				string message = string.Format("RPC unable to find Method with key \"{0}\" for type {1}", Key,
				                               client.GetType().Name);
				throw new KeyNotFoundException(message);
			}

			try
			{
				method.Invoke(client, m_Parameters.ToArray());
			}
			catch (Exception e)
			{
#if SIMPLSHARP
				throw e.InnerException ?? e;
#else
				ExceptionDispatchInfo.Capture(e.InnerException ?? e).Throw();
				throw;
#endif
			}
		}

		/// <summary>
		/// Executes the RPC as a property setter on the given client.
		/// </summary>
		/// <param name="client"></param>
		private void ExecutePropertySetter(object client)
		{
			PropertyInfo property = RpcAttribute.GetProperty(client, Key, m_Parameters.First());

			if (property == null)
			{
				string message = string.Format("RPC unable to find Property with key \"{0}\" for type {1}", Key,
				                               client.GetType().Name);
				throw new KeyNotFoundException(message);
			}

			try
			{
				property.SetValue(client, m_Parameters[0], null);
			}
			catch (Exception e)
			{
#if SIMPLSHARP
				throw e.InnerException ?? e;
#else
				ExceptionDispatchInfo.Capture(e.InnerException ?? e).Throw();
				throw;
#endif
			}
		}

		#endregion
	}

	public sealed class RpcConverter : AbstractGenericJsonConverter<Rpc>
	{
		private const string PROCEDURE_TYPE_TOKEN = "t";
		private const string KEY_TOKEN = "k";
		private const string PARAMETERS_TOKEN = "p";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, Rpc value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.ProcedureType != default(Rpc.eProcedureType))
				writer.WriteProperty(PROCEDURE_TYPE_TOKEN, (int)value.ProcedureType);

			if (value.Key != null)
				writer.WriteProperty(KEY_TOKEN, value.Key);

			writer.WritePropertyName(PARAMETERS_TOKEN);
			serializer.SerializeArray(writer, value.GetParameters().Select(p => new JsonItemWrapper(p)));
		}

		/// <summary>
		/// Override to handle the current property value with the given name.
		/// </summary>
		/// <param name="property"></param>
		/// <param name="reader"></param>
		/// <param name="instance"></param>
		/// <param name="serializer"></param>
		protected override void ReadProperty(string property, JsonReader reader, Rpc instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case PROCEDURE_TYPE_TOKEN:
					instance.ProcedureType = (Rpc.eProcedureType)reader.GetValueAsInt();
					break;

				case KEY_TOKEN:
					instance.Key = reader.GetValueAsString();
					break;

				case PARAMETERS_TOKEN:
					IEnumerable<JsonItemWrapper> wrappers =
						serializer.DeserializeArray<JsonItemWrapper>(reader) ?? Enumerable.Empty<JsonItemWrapper>();
					instance.SetParameters(wrappers.Select(w => w.Item));
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
