using MultiFactor.Ldap.Adapter.Configuration;
using MultiFactor.Ldap.Adapter.Services;
using Serilog;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace MultiFactor.Ldap.Adapter.Core
{
    public static class TlsCertificateFactory
    {
        public static void EnsureTlsCertificatesExist(string path, ServiceConfiguration configuration, ILogger logger)
        {
            var certDirectory = $"{path}tls";
            if (!Directory.Exists(certDirectory))
            {
                Directory.CreateDirectory(certDirectory);
            }

            var certPath = $"{certDirectory}{Path.DirectorySeparatorChar}certificate.pfx";
            if (!File.Exists(certPath))
            {
                var subj = Dns.GetHostEntry("").HostName;

                logger.Debug($"Generating self-signing certificate for TLS with subject CN={subj}");

                var certService = new CertificateService();
                var cert = certService.GenerateCertificate(subj);

                var data = cert.Export(X509ContentType.Pfx);
                File.WriteAllBytes(certPath, data);

                logger.Information($"Self-signed certificate with subject CN={subj} saved to {certPath}");

                configuration.X509Certificate = cert;
            }
            else
            {
                if (!string.IsNullOrEmpty(configuration.CertificatePassword))
                {
                    logger.Debug($"Loading certificate for TLS from {certPath} with CertificatePassword XXX");
                    configuration.X509Certificate = new X509Certificate2(certPath, configuration.CertificatePassword);
                }   
                else
                {
                    logger.Debug($"Loading certificate for TLS from {certPath}");
                    configuration.X509Certificate = new X509Certificate2(certPath);
                }

            }
        }
    }
}
