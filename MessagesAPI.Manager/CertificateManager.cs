using MessagesAPI.Manager.Properties;
using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace MessagesAPI.Manager
{
    public class CertificateManager
    {
        public static bool AllwaysGoodCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors policyErrors)
        {
            return true;
        }


        public static X509Certificate2 GetCertificateByThumbprint(string certificateThumbPrint, StoreLocation certificateStoreLocation)
        {
            X509Certificate2 certificate = null;

            X509Store certificateStore = new X509Store(certificateStoreLocation);
            certificateStore.Open(OpenFlags.ReadOnly);

            X509Certificate2Collection certificates = certificateStore.Certificates;
            foreach (X509Certificate2 cert in certificates)
            {
                if (cert.Thumbprint != null && cert.Thumbprint.Equals(certificateThumbPrint, StringComparison.OrdinalIgnoreCase))
                {
                    certificate = cert;
                    break;
                }
            }

            if (certificate == null)
            {
                return null;
                //Log.ErrorFormat(CultureInfo.InvariantCulture, "Certificate with thumbprint {0} not found", certificateThumbPrint);
            }

            certificate = new X509Certificate2(certificate.GetRawCertData(), Settings.Default.X509DataPassword);
            return certificate;
        }

    }
}
