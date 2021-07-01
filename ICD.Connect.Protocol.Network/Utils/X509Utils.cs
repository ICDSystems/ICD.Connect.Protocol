using System;
using ICD.Common.Properties;
using ICD.Common.Utils.IO;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using X509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace ICD.Connect.Protocol.Network.Utils
{
	public static class X509Utils
	{
		#region Members

		private static readonly SecureRandom s_SecureRandom = new SecureRandom();

		#endregion

		#region Methods

		/// <summary>
		/// Generates and writes a PFX file to the specified path.
		/// </summary>
		/// <param name="commonName"></param>
		/// <param name="path"></param>
		[PublicAPI]
		public static void GenerateAndWriteCertificate(string commonName, string path)
		{
			X509Name name = new X509Name(string.Format("CN={0}", commonName));
			AsymmetricCipherKeyPair key = GenerateRsaKeyPair(2048);
			X509Certificate cert = Generate(name, name, key.Private, key.Public);

			var store = new Pkcs12Store();
			var certificateEntry = new X509CertificateEntry(cert);

			store.SetCertificateEntry(commonName, certificateEntry);
			store.SetKeyEntry(commonName, new AsymmetricKeyEntry(key.Private), new [] {certificateEntry});

			using (var f = IcdFile.Create(path))
			{
				var stream = new IcdMemoryStream();
				store.Save(stream.WrappedMemoryStream, null, s_SecureRandom);
				var byteArray = stream.WrappedMemoryStream.ToArray();
				f.WrappedFileStream.Write(byteArray, 0, byteArray.Length);
			}
		}
		#endregion

		#region Private Methods

		/// <summary>
		/// Generates an RSA private key.
		/// </summary>
		/// <param name="length"></param>
		/// <returns></returns>
		private static AsymmetricCipherKeyPair GenerateRsaKeyPair(int length)
		{
			var keygenParam = new KeyGenerationParameters(s_SecureRandom, length);

			var keyGenerator = new RsaKeyPairGenerator();
			keyGenerator.Init(keygenParam);
			return keyGenerator.GenerateKeyPair();
		}

		/// <summary>
		/// Generates an X509 Certificate.
		/// </summary>
		/// <param name="issuer"></param>
		/// <param name="subject"></param>
		/// <param name="issuerPrivate"></param>
		/// <param name="subjectPublic"></param>
		/// <returns></returns>
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

			// Serial number needs to be an unsigned integer
			// Should be unique every time - not a device serial number
			long hash = (long)Guid.NewGuid().GetHashCode() + (long)int.MaxValue;
			uint unsignedHash = (uint)hash;

			X509V3CertificateGenerator certGenerator = new X509V3CertificateGenerator();
			certGenerator.SetIssuerDN(issuer);
			certGenerator.SetSubjectDN(subject);
			certGenerator.SetSerialNumber(new BigInteger(unsignedHash.ToString()));
			certGenerator.SetNotAfter(DateTime.UtcNow.AddYears(20));
			certGenerator.SetNotBefore(DateTime.UtcNow);
			certGenerator.SetPublicKey(subjectPublic);
			return certGenerator.Generate(signatureFactory);
		}

		#endregion
	}
}