using System;
using System.Collections.Generic;
using System.Text;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Protocol.Network.Ports.Mqtt
{
	public static class MqttClientConsole
	{
		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleNodeBase> GetConsoleNodes(IMqttClient instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			yield break;
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="addRow"></param>
		public static void BuildConsoleStatus(IMqttClient instance, AddStatusRowDelegate addRow)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			addRow("Hostname", instance.Hostname);
			addRow("Port", instance.Port);
			addRow("ProxyHostname", instance.ProxyHostname);
			addRow("ProxyPort", instance.ProxyPort);
			addRow("ClientId", instance.ClientId);
			addRow("Username", instance.Username);
			addRow("Password", instance.Password);
			addRow("Secure", instance.Secure);
			addRow("CaCertPath", instance.CaCertPath);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleCommand> GetConsoleCommands(IMqttClient instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			yield return new GenericConsoleCommand<string, string, byte, bool>("Publish", "Publish <TOPIC> <MESSAGE> <QOS> <RETAIN>",
			                                                       (t, m, q, r) =>
			                                                       {
																	   byte[] bytes = Encoding.UTF8.GetBytes(m);
				                                                       instance.Publish(t, bytes, q, r);
			                                                       });
			yield return new GenericConsoleCommand<string>("Clear", "Clear <TOPIC>", t => instance.Clear(t));

			yield return new GenericConsoleCommand<string>("SetHostname", "SetHostname <HOSTNAME>", h => instance.Hostname = h);
			yield return new GenericConsoleCommand<ushort>("SetPort", "SetPort <PORT>", p => instance.Port = p);
			yield return new GenericConsoleCommand<string>("SetProxyHostname", "SetProxyHostname <HOSTNAME>", h => instance.ProxyHostname = h);
			yield return new GenericConsoleCommand<string>("SetProxyPort", "SetProxyPort <PORT>", h => instance.Hostname = h);
		}
	}
}
