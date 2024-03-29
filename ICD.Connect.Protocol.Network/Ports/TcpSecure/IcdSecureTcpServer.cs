﻿using ICD.Common.Utils;
using ICD.Connect.Protocol.Network.Ports.Tcp;

namespace ICD.Connect.Protocol.Network.Ports.TcpSecure
{
	public sealed partial class IcdSecureTcpServer : AbstractTcpServer
	{
		private const string ICD_SECURE_TCP_SERVER_COMMON_NAME = "ICDAutoGeneratedCertificate";
		private const string AUTO_GENERATED_CERTIFICATE_PATH_PREFIX = "ICDAutoGeneratedCertificate";
		private const string AUTO_GENERATED_X509_PATH_PREFIX = "IcdTempCert";
		private const string KEY_PATH_PREFIX = "IcdTempKey";
		private const string PFX_EXTENSION = ".pfx";
		private const string CER_EXTENSION = ".cer";
		private const string KEY_EXTENSION = ".key";

		private static string AutoGeneratedPfxPath()
		{
			return PathUtils.Join(PathUtils.ProgramDataPath,
			                      string.Concat(AUTO_GENERATED_CERTIFICATE_PATH_PREFIX, PFX_EXTENSION));
		}

		private static string CerPath()
		{
			return PathUtils.Join(PathUtils.ProgramDataPath,
			                      string.Concat(AUTO_GENERATED_X509_PATH_PREFIX, CER_EXTENSION));
		}

		private static string KeyPath()
		{
			return PathUtils.Join(PathUtils.ProgramDataPath,
			                      string.Concat(KEY_PATH_PREFIX, KEY_EXTENSION));
		}
	}
}