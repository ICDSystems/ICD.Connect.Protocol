using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Protocol.Network.Servers
{
	public interface INetworkServer : IDisposable, IConsoleNode
	{
		/// <summary>
		/// Raised when data is received from a client.
		/// </summary>
		event EventHandler<DataReceiveEventArgs> OnDataReceived;

		/// <summary>
		/// Raised when a client socket state changes.
		/// </summary>
		event EventHandler<SocketStateEventArgs> OnSocketStateChange;

		/// <summary>
		/// Raised when the server starts/stops listening.
		/// </summary>
		event EventHandler<BoolEventArgs> OnListeningStateChanged;

		/// <summary>
		/// IP Address to accept connection from.
		/// </summary>
		[PublicAPI]
		string AddressToAcceptConnectionFrom { get; set; }

		/// <summary>
		/// Port for server to listen on.
		/// </summary>
		[PublicAPI]
		ushort Port { get; set; }

		/// <summary>
		/// Get or set the receive buffer size.
		/// </summary>
		[PublicAPI]
		int BufferSize { get; set; }

		/// <summary>
		/// Tracks the enabled state of the server between getting/losing network connection.
		/// </summary>
		[PublicAPI]
		bool Enabled { get; }

		/// <summary>
		/// Gets the listening state of the server.
		/// </summary>
		[PublicAPI]
		bool Listening { get; }

		/// <summary>
		/// Max number of connections supported by the server.
		/// </summary>
		[PublicAPI]
		int MaxNumberOfClients { get; set; }

		/// <summary>
		/// Number of active connections.
		/// </summary>
		int NumberOfClients { get; }

		/// <summary>
		/// Assigns a name to the server for use with logging.
		/// </summary>
		string Name { get; set; }

		/// <summary>
		/// When enabled prints the received data to the console.
		/// </summary>
		[PublicAPI]
		eDebugMode DebugRx { get; set; }

		/// <summary>
		/// When enabled prints the transmitted data to the console.
		/// </summary>
		[PublicAPI]
		eDebugMode DebugTx { get; set; }

		/// <summary>
		/// Stops and starts the server.
		/// </summary>
		void Restart();

		/// <summary>
		/// Gets the active client ids.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		IEnumerable<uint> GetClients();

		/// <summary>
		/// Starts the TCP Server
		/// </summary>
		[PublicAPI]
		void Start();

		/// <summary>
		/// Stops and Disables the TCP Server
		/// </summary>
		void Stop();

		/// <summary>
		/// Sends the data to all connected clients.
		/// </summary>
		/// <param name="data"></param>
		void Send(string data);

		/// <summary>
		/// Sends a Byte for Byte string (ISO-8859-1)
		/// </summary>
		/// <param name="clientId">Client Identifier for Connection</param>
		/// <param name="data">String in ISO-8859-1 Format</param>
		/// <returns></returns>
		void Send(uint clientId, string data);

		/// <summary>
		/// Gets the address and port for the client with the given id.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="info"></param>
		/// <returns></returns>
		[PublicAPI]
		bool TryGetClientInfo(uint client, out HostInfo info);

		/// <summary>
		/// Returns true if the client is connected.
		/// </summary>
		/// <param name="client"></param>
		/// <returns></returns>
		bool ClientConnected(uint client);
	}
}
