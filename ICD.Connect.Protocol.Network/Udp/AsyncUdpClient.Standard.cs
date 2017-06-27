
#if !SIMPLSHARP
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Connect.Protocol.Ports;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ICD.Connect.Protocol.Network.Udp
{
    public sealed partial class AsyncUdpClient : AbstractSerialPort<AsyncUdpClientSettings>
    {
        private UdpClient m_UdpClient;

        /// <summary>
        /// Address to accept connections from.
        /// </summary>
        [PublicAPI]
        public string Address
        {
            get { return m_Address; }
            set { m_Address = value; }
        }

        /// <summary>
        /// Connects to the end point.
        /// </summary>
        public override void Connect()
        {
            Disconnect();

            m_UdpClient = new UdpClient(Port);
            m_UdpClient.EnableBroadcast = true;
            m_UdpClient.JoinMulticastGroup(IPAddress.Parse(Address));

            // Spawn new listening thread
            m_ListeningRequested = true;
            m_UdpClient.ReceiveAsync().ContinueWith(UdpClientReceiveHandler);

            UpdateIsConnectedState();
        }

        /// <summary>
        /// Disconnects from the end point.
        /// </summary>
        public override void Disconnect()
        {
            m_ListeningRequested = false;

            if (m_UdpClient != null)
            {
                m_UdpClient.DropMulticastGroup(IPAddress.Parse(Address));
                m_UdpClient.Dispose();
            }
            m_UdpClient = null;

            UpdateIsConnectedState();
        }

        /// <summary>
        /// Returns the connection state of the port
        /// </summary>
        /// <returns></returns>
        protected override bool GetIsConnectedState()
        {
            if (m_UdpClient == null)
                return false;

            return true;
        }

        /// <summary>
        /// Implements the actual sending logic. Wrapped by SendToAddress to handle connection status.
        /// </summary>
        private bool SendToAddressFinal(string data, string ipAddress, int port)
        {
            byte[] bytes = StringUtils.ToBytes(data);

            m_UdpClient.SendAsync(bytes, bytes.Length, ipAddress, port).Wait();
            PrintTx(data);

            return true;
        }

        /// <summary>
        /// Implements the actual sending logic. Wrapped by Send to handle connection status.
        /// </summary>
        protected override bool SendFinal(string data)
        {
            byte[] bytes = StringUtils.ToBytes(data);

            PrintTx(data);
            m_UdpClient.Client.Send(bytes, bytes.Length, SocketFlags.Broadcast);

            return true;
        }

        /// <summary>
        /// Handles Receiving Data from the Active TCP Connection
        /// </summary>
        /// <param name="udpClient"></param>
        /// <param name="bytesReceived"></param>
        private void UdpClientReceiveHandler(Task<UdpReceiveResult> task)
        {
            var result = task.Result;
            if (result.Buffer.Length <= 0)
                return;

            string data = StringUtils.ToString(result.Buffer);

            PrintRx(data);
            Receive(data);

            m_UdpClient.ReceiveAsync().ContinueWith(UdpClientReceiveHandler);

            UpdateIsConnectedState();
        }
    }
}
#endif
