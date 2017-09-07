
#if STANDARD
using ICD.Common.Properties;
using ICD.Common.Utils;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using ICD.Common.Services.Logging;
using ICD.Connect.Protocol.Ports;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Protocol.Network.Tcp
{
    public sealed partial class AsyncTcpServer
    {
        private TcpListener m_TcpListener;

        private Dictionary<uint, TcpClient> m_Clients = new Dictionary<uint, TcpClient>();
        private Dictionary<uint, byte[]> m_ClientBuffers = new Dictionary<uint, byte[]>();

        /// <summary>
        /// Starts the TCP Server
        /// </summary>
        [PublicAPI]
        public void Start()
        {
            Active = true;

            m_TcpListener = new TcpListener(IPAddress.Any, Port);
            m_TcpListener.Start();
            m_TcpListener.AcceptTcpClientAsync().ContinueWith(TcpClientConnectCallback);

            Logger.AddEntry(eSeverity.Notice, string.Format("AsyncTcpServer Listening on Port {0} with Max # of Connections {1}", Port,
                                             MaxNumberOfClients));
        }

        /// <summary>
        /// Stops and Disables the TCP Server
        /// </summary>
        [PublicAPI]
        public void Stop()
        {
            Active = false;

            if (m_TcpListener != null)
            {
                m_TcpListener.Stop();
                Logger.AddEntry(eSeverity.Notice, string.Format("AsyncTcpServer No Longer Listening on Port {0}", (m_TcpListener.LocalEndpoint as IPEndPoint).Port));
            }
            m_TcpListener = null;

            foreach (uint client in GetClients())
                RemoveClient(client);
        }

        /// <summary>
        /// Sends the data to all connected clients.
        /// </summary>
        /// <param name="data"></param>
        public void Send(string data)
        {
            byte[] byteData = StringUtils.ToBytes(data);
            foreach (var client in m_Clients.Values)
                if (client.Connected)
                    client.Client.Send(byteData, 0, byteData.Length, SocketFlags.None);
        }

        /// <summary>
        /// Sends a Byte for Byte string (ISO-8859-1)
        /// </summary>
        /// <param name="clientId">Client Identifier for Connection</param>
        /// <param name="data">String in ISO-8859-1 Format</param>
        /// <returns></returns>
        public void Send(uint clientId, string data)
        {
            if (!ClientConnected(clientId))
            {
                string message = string.Format("{0} unable to send data to unconnected client {1}", GetType().Name, clientId);
                throw new InvalidOperationException(message);
            }

            byte[] byteData = StringUtils.ToBytes(data);

            m_Clients[clientId].Client.Send(byteData, 0, byteData.Length, SocketFlags.None);
        }

        /// <summary>
        /// Gets the address and port for the client with the given id.
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        [PublicAPI]
        public HostInfo GetClientInfo(uint clientId)
        {
            var client = m_Clients[clientId];
            var endpoint = client.Client.RemoteEndPoint as IPEndPoint;

            return new HostInfo(endpoint.Address.ToString(), (ushort)endpoint.Port);
        }

        /// <summary>
        /// Returns true if the client is connected.
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool ClientConnected(uint client)
        {
            // This is a hack. We have no way of determining if a client id is still valid,
            // so if we get a null address we know the client is invalid.
            if (!m_Clients.ContainsKey(client) || m_Clients[client] == null)
                return false;

            return m_Clients[client].Connected;
        }

        /// <summary>
        /// Handles an incoming TCP connection
        /// </summary>
        /// <param name="tcpClient"></param>
        /// <param name="clientId"></param>
        private void TcpClientConnectCallback(Task<TcpClient> tcpClient)
        {
            var clientId = (uint)m_Clients.Count + 1;
            m_Clients[clientId] = tcpClient.Result;
            m_ClientBuffers[clientId] = new byte[16384];
            AddClient(clientId);
            m_Clients[clientId].GetStream().ReadAsync(m_ClientBuffers[clientId], 0, 16384).ContinueWith(a => TcpClientReceiveHandler(a, clientId));
            // Spawn new thread for accepting new clients
            m_TcpListener.AcceptTcpClientAsync().ContinueWith(TcpClientConnectCallback);
        }

        /// <summary>
        /// Handles receiving data from a specific client
        /// </summary>
        /// <param name="tcpListener"></param>
        /// <param name="clientId"></param>
        /// <param name="bytesReceived"></param>
        private void TcpClientReceiveHandler(Task<int> task, uint clientId)
        {
            
            if (clientId == 0)
                return;

            byte[] buffer = m_ClientBuffers[clientId];

            OnDataReceived.Raise(null, new TcpReceiveEventArgs(clientId, buffer, task.Result));

            if (!ClientConnected(clientId))
            {
                RemoveClient(clientId);
                return;
            }

            // Spawn a new listening thread
            m_Clients[clientId].GetStream().ReadAsync(buffer, 0, 16384).ContinueWith(a => TcpClientReceiveHandler(a, clientId));
            //if (socketError == SocketErrorCodes.SOCKET_OPERATION_PENDING)
                //return;

            //Logger.AddEntry(eSeverity.Error, "AsyncTcpServer[ClientId({0}) RemoteClient({1})] failed to ReceiveDataAsync: {2}",
                    //clientId, GetHostnameForClientId(clientId), socketError);

            //RemoveClient(clientId);
            
        }

        /// <summary>
        /// Gets the hostname for the client in the format 0.0.0.0:0
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        private string GetHostnameForClientId(uint clientId)
        {
            return GetClientInfo(clientId).ToString();
        }
    }
}
#endif
