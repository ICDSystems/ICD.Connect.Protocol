using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Protocol.Network.WebPorts.Https
{
	/// <summary>
	/// Allows communication with a device over HTTPS.
	/// </summary>
	public sealed partial class HttpsPort : AbstractWebPort<HttpsPortSettings>
	{
		public enum eCertificateType
		{
			Unknown = 0,
			Pem = 1,
			Der = 2,
			P12 = 3
		}

		private string m_Certificate;
		private string m_CertificatePassword;
		private eCertificateType m_CertificateType;

		/// <summary>
		/// The request protocol, i.e. http or https.
		/// </summary>
		protected override string Protocol { get { return "https"; } }

		#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(HttpsPortSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Certificate = m_Certificate;
			settings.CertificatePassword = m_CertificatePassword;
			settings.CertificateType = m_CertificateType;
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			m_Certificate = null;
			m_CertificatePassword = null;
			m_CertificateType = default(eCertificateType);
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(HttpsPortSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			// Load the certificate
			if (!string.IsNullOrEmpty(settings.Certificate))
				LoadClientCertificate(settings.Certificate, settings.CertificatePassword, settings.CertificateType);
		}

		/// <summary>
		/// Loads the SSL certificate from the given path.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="password"></param>
		/// <param name="type"></param>
		[PublicAPI]
		public void LoadClientCertificate(string path, string password, eCertificateType type)
		{
			m_Certificate = path;
			m_CertificatePassword = password;
			m_CertificateType = type;

			string fullPath = PathUtils.GetSslCertificatesPath(path);

			LoadClientCertificateFinal(fullPath, password, type);
		}

		#endregion

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Certificate", m_Certificate);
			addRow("Certificate Password", StringUtils.PasswordFormat(m_CertificatePassword));
			addRow("Certificate Type", m_CertificateType);
		}

		/// <summary>
		/// Gets the console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			string help = string.Format("LoadCertificate <PATH> <PASSWORD> <{0}>",
			                            StringUtils.ArrayFormat(EnumUtils.GetValuesExceptNone<eCertificateType>()));

			yield return
				new GenericConsoleCommand<string, string, eCertificateType>("LoadCertificate", help,
				                                                            (a, b, c) => LoadClientCertificate(a, b, c));
		}

		/// <summary>
		/// Workaround to avoid "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
