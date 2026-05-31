using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
public class CertGenerator
{
    public static void GenerateCert(string name)
    {
        var certFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cert");
        if (!Directory.Exists(certFolder))
        {
            Directory.CreateDirectory(certFolder);
        }
        using (RSA rsa = RSA.Create(2048))
        {
            var request = new CertificateRequest("CN=tm.eugnet.com", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddDnsName("tm.eugnet.com");
            request.CertificateExtensions.Add(sanBuilder.Build());

            // 1. Метод CreateSelfSigned возвращает сертификат, в который УЖЕ вшит приватный ключ 'rsa'
            using (X509Certificate2 cert = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(99)))
            {
                // 2. Экспортируем напрямую из cert (без всяких CopyWithPrivateKey)
                byte[] pfxBytes = cert.Export(X509ContentType.Pfx, "");

                File.WriteAllBytes(Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cert"), name), pfxBytes);
            }
        }
    }
}