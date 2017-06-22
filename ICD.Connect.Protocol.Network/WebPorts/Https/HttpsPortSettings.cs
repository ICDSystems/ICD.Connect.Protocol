using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Xml;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Attributes.Factories;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Protocol.Network.WebPorts.Https
{
	/// <summary>
	/// Settings for a HttpsPort.
	/// </summary>
	public sealed class HttpsPortSettings : AbstractWebPortSettings
	{
		private const string FACTORY_NAME = "HTTPS";

		private const string CERTIFICATE_ELEMENT = "Certificate";
		private const string CERTIFICATE_PASSWORD_ELEMENT = "CertificatePassword";
		private const string CERTIFICATE_TYPE_ELEMENT = "CertificateType";

		#region Properties

		public string Certificate { get; set; }
		public string CertificatePassword { get; set; }
		public HttpsPort.eCertificateType CertificateType { get; set; }

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		#endregion

		#region Methods

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			if (string.IsNullOrEmpty(Certificate))
				return;

			writer.WriteElementString(CERTIFICATE_ELEMENT, Certificate);

			if (!string.IsNullOrEmpty(CertificatePassword))
				writer.WriteElementString(CERTIFICATE_PASSWORD_ELEMENT, CertificatePassword);

			if (CertificateType != default(HttpsPort.eCertificateType))
				writer.WriteElementString(CERTIFICATE_TYPE_ELEMENT, CertificateType.ToString());
		}

		/// <summary>
		/// Creates a new originator instance from the settings.
		/// </summary>
		/// <param name="factory"></param>
		/// <returns></returns>
		public override IOriginator ToOriginator(IDeviceFactory factory)
		{
			HttpsPort output = new HttpsPort();
			output.ApplySettings(this, factory);

			return output;
		}

		/// <summary>
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlPortSettingsFactoryMethod(FACTORY_NAME)]
		public static HttpsPortSettings FromXml(string xml)
		{
			string certificate = XmlUtils.TryReadChildElementContentAsString(xml, CERTIFICATE_ELEMENT);
			string certificatePassword = XmlUtils.TryReadChildElementContentAsString(xml, CERTIFICATE_PASSWORD_ELEMENT);
			string certificateTypeString = XmlUtils.TryReadChildElementContentAsString(xml, CERTIFICATE_TYPE_ELEMENT);

			HttpsPort.eCertificateType certificateType;
			EnumUtils.TryParse(certificateTypeString, true, out certificateType);

			HttpsPortSettings output = new HttpsPortSettings
			{
				Certificate = certificate,
				CertificatePassword = certificatePassword,
				CertificateType = certificateType
			};

			ParseXml(output, xml);
			return output;
		}

		#endregion
	}
}
