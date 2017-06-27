
#if STANDARD
namespace ICD.Connect.Protocol.Network.WebPorts.Http
{
    public sealed partial class HttpPort
    {
        public override string Accept { get; set; }
        public override string Username { get; set; }
        public override string Password { get; set; }
        public override bool Busy { get; }
        public override string Get(string localUrl)
        {
            throw new System.NotImplementedException();
        }

        public override string Post(string localUrl, byte[] data)
        {
            throw new System.NotImplementedException();
        }

        public override string DispatchSoap(string action, string content)
        {
            throw new System.NotImplementedException();
        }
    }
}
#endif
