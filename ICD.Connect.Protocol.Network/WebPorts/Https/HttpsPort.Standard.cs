
#if STANDARD
using System;

namespace ICD.Connect.Protocol.Network.WebPorts.Https
{
    public sealed partial class HttpsPort
    {
        public override string Accept { get; set; }
        public override string Username { get; set; }
        public override string Password { get; set; }
        public override bool Busy { get { throw new NotImplementedException(); } }

        /// <summary>
        /// Loads the SSL certificate from the given path.
        /// </summary>
        /// <param name="fullPath"></param>
        /// <param name="password"></param>
        /// <param name="type"></param>
        public void LoadClientCertificateFinal(string fullPath, string password, eCertificateType type)
        {
            throw new NotImplementedException();
        }

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
