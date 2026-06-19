using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

public class CertGenerator
{
    public static void GenerateCert(string fileName)
    {
        var sslDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SSL");

        Directory.CreateDirectory(sslDir);

        var fullPath = Path.Combine(sslDir, fileName);

        using (RSA rsa = RSA.Create(2048))
        {
            var request = new CertificateRequest(
                "CN=tm.eugnet.com",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1
            );

            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddDnsName("tm.eugnet.com");
            request.CertificateExtensions.Add(sanBuilder.Build());

            using (X509Certificate2 cert =
                   request.CreateSelfSigned(
                       DateTimeOffset.UtcNow,
                       DateTimeOffset.UtcNow.AddYears(10)))
            {
                byte[] pfxBytes = cert.Export(X509ContentType.Pfx, "");
                File.WriteAllBytes(fullPath, pfxBytes);
            }
        }
    }
}