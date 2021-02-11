#if SIMPLSHARP
using Crestron.SimplSharp.CrestronIO;
#else
using System.IO;
#endif
using System;
using System.Collections.Generic;
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
using Org.BouncyCastle.OpenSsl;
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

		/// <summary>
		/// Extracts the X509 Certificate and private key bytes from a PFX file.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="cerPath"></param>
		/// <param name="keyPath"></param>
		/// <returns></returns>
		[PublicAPI]
		public static KeyValuePair<byte[], byte[]> GetCertAndKeyFromPfx(string path, string cerPath, string keyPath)
		{
			using (IcdFileStream fs = new IcdFileStream(new FileStream(path, FileMode.Open)))
			{
				Pkcs12Store store = new Pkcs12Store(fs.WrappedFileStream, new char[0]);

				foreach (string alias in store.Aliases)
				{
					if (store.IsKeyEntry(alias) && store.GetKey(alias).Key.IsPrivate)
					{
						X509Certificate cert = store.GetCertificate(alias).Certificate;
						AsymmetricKeyParameter privateKey = store.GetKey(alias).Key;

						using (var f = IcdFile.Create(cerPath))
						{
							var buf = cert.GetEncoded();
							f.WrappedFileStream.Write(buf, 0, buf.Length);
						}

						using (IcdStream s = new IcdStream(new FileStream(keyPath, FileMode.Create)))
							using (IcdTextWriter tw = new IcdStreamWriter(new StreamWriter(s.WrappedStream)))
							{
								var generator = new MiscPemGenerator(privateKey);
								PemWriter pemWriter = new PemWriter(tw.WrappedTextWriter);
								pemWriter.WriteObject(generator);
								tw.WrappedTextWriter.Flush();
							}
					}
					break;
				}

				var certBytes = ReadFileToByteArray(cerPath);
				var keyBytes = ReadFileToByteArray(keyPath);

				return new KeyValuePair<byte[], byte[]>(certBytes, keyBytes);
			}
		} 

		#endregion

		#region Private Methods

		/// <summary>
		/// Reads the file from the specified path into a byte array.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private static byte[] ReadFileToByteArray(string path)
		{
			byte[] buffer;
			using (IcdFileStream fs = new IcdFileStream(new FileStream(path, FileMode.Open, FileAccess.Read)))
			{
				buffer = new byte[fs.Length];
				fs.WrappedFileStream.Read(buffer, 0, (int)fs.Length);
			}
			return buffer;
		}

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

			X509V3CertificateGenerator certGenerator = new X509V3CertificateGenerator();
			certGenerator.SetIssuerDN(issuer);
			certGenerator.SetSubjectDN(subject);
			certGenerator.SetSerialNumber(new BigInteger(Guid.NewGuid().GetHashCode().ToString()));
			certGenerator.SetNotAfter(DateTime.UtcNow.AddYears(20));
			certGenerator.SetNotBefore(DateTime.UtcNow);
			certGenerator.SetPublicKey(subjectPublic);
			return certGenerator.Generate(signatureFactory);
		}

		#endregion
	}
}