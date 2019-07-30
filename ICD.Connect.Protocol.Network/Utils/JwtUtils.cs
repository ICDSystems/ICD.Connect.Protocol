using System;
using System.Collections.Generic;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace ICD.Connect.Protocol.Network.Utils
{
	public static class JwtUtils
	{
		public static string SignRs256(string payload, string privateKey)
		{
			List<string> segments = new List<string>();

			// Encode the header
			const string header = @"{""alg"":""RS256"",""typ"":""JWT""}";
			byte[] headerBytes = Encoding.UTF8.GetBytes(header);
			segments.Add(Base64UrlEncode(headerBytes));

			// Encode the payload
			byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);
			segments.Add(Base64UrlEncode(payloadBytes));

			// Get the header.payload for signature hash
			string stringToSign = string.Join(".", segments.ToArray());
			byte[] bytesToSign = Encoding.UTF8.GetBytes(stringToSign);

			// Build the signature
			byte[] keyBytes = Convert.FromBase64String(privateKey);

			AsymmetricKeyParameter asymmetricKeyParameter = PrivateKeyFactory.CreateKey(keyBytes);
			RsaKeyParameters rsaKeyParameter = (RsaKeyParameters)asymmetricKeyParameter;
			ISigner sig = SignerUtilities.GetSigner("SHA256withRSA");
			sig.Init(true, rsaKeyParameter);

			sig.BlockUpdate(bytesToSign, 0, bytesToSign.Length);
			byte[] signature = sig.GenerateSignature();

			segments.Add(Base64UrlEncode(signature));
			return string.Join(".", segments.ToArray());
		}

		// from JWT spec
		private static string Base64UrlEncode(byte[] input)
		{
			string output = Convert.ToBase64String(input);
			output = output.Split('=')[0]; // Remove any trailing '='s
			output = output.Replace('+', '-'); // 62nd char of encoding
			output = output.Replace('/', '_'); // 63rd char of encoding
			return output;
		}
	}
}
