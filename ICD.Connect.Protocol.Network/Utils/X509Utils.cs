using System;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace ICD.Connect.Protocol.Network.Utils
{
	public static class X509Utils
	{
		#region Members

		private static readonly SecureRandom s_SecureRandom = new SecureRandom();

		#endregion

		#region Methods

		public static X509Certificate GenerateCertificate(string commonName)
		{
			var name = new X509Name(string.Format("CN={0}", commonName));
			var key = GenerateRsaKeyPair(2048);
			return Generate(name, name, key.Private, key.Public);
		}

		#endregion

		#region Private Methods

		private static AsymmetricCipherKeyPair GenerateRsaKeyPair(int length)
		{
			var keygenParam = new KeyGenerationParameters(s_SecureRandom, length);

			var keyGenerator = new RsaKeyPairGenerator();
			keyGenerator.Init(keygenParam);
			return keyGenerator.GenerateKeyPair();
		}

		private static X509Certificate Generate(
			X509Name issuer, X509Name subject,
			AsymmetricKeyParameter issuerPrivate,
			AsymmetricKeyParameter subjectPublic)
		{
			ISignatureFactory signatureFactory;
			if (issuerPrivate is ECPrivateKeyParameters)
			{
				signatureFactory = new Asn1SignatureFactory(
					X9ObjectIdentifiers.ECDsaWithSha256.ToString(),
					issuerPrivate);
			}
			else
			{
				signatureFactory = new Asn1SignatureFactory(
					PkcsObjectIdentifiers.Sha256WithRsaEncryption.ToString(),
					issuerPrivate);
			}

			var certGenerator = new X509V3CertificateGenerator();
			certGenerator.SetIssuerDN(issuer);
			certGenerator.SetSubjectDN(subject);
			certGenerator.SetSerialNumber(BigInteger.ValueOf(1));
			certGenerator.SetNotAfter(DateTime.UtcNow.AddHours(1));
			certGenerator.SetNotBefore(DateTime.UtcNow);
			certGenerator.SetPublicKey(subjectPublic);
			return certGenerator.Generate(signatureFactory);
		}

		#endregion
	}
}